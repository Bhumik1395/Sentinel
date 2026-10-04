// backend/src/Sentinel.Identity/Onboarding/ApplicationsService.cs
using Npgsql;
using Sentinel.Identity.Data;
using Sentinel.Identity.Organizations;

namespace Sentinel.Identity.Onboarding;

public interface IApplicationsService
{
    Task<Guid> SubmitApplicationAsync(SubmitApplicationRequest request);
    Task<IReadOnlyList<Application>> ListApplicationsAsync();
    Task MarkInReviewAsync(Guid applicationId, Guid? reviewerUserId);
    Task MarkInDiscussionAsync(Guid applicationId);
    Task<ApproveApplicationResult> ApproveApplicationAsync(Guid applicationId);
    Task RejectApplicationAsync(Guid applicationId, string reason);
}

public class ApplicationsService : IApplicationsService
{
    private static readonly string[] OpenStatuses = { "PENDING", "IN_REVIEW", "IN_DISCUSSION" };

    private readonly ISentinelDataSource _dataSource;
    private readonly IOrganizationsService _organizations;

    public ApplicationsService(ISentinelDataSource dataSource, IOrganizationsService organizations)
    {
        _dataSource = dataSource;
        _organizations = organizations;
    }

    public async Task<Guid> SubmitApplicationAsync(SubmitApplicationRequest request)
    {
        if (request.RequestedEndpointCap <= 0)
            throw new ArgumentException("requestedEndpointCap must be a positive integer.");
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            throw new ArgumentException("companyName is required.");
        if (string.IsNullOrWhiteSpace(request.ContactName))
            throw new ArgumentException("contactName is required.");
        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            throw new ArgumentException("contactEmail is required.");

        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO applications
                (id, company_name, contact_name, contact_email, requested_endpoint_cap, notes)
              VALUES (@id, @company, @contact, @email, @cap, @notes)",
            conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("company", request.CompanyName.Trim());
        cmd.Parameters.AddWithValue("contact", request.ContactName.Trim());
        cmd.Parameters.AddWithValue("email", request.ContactEmail.Trim());
        cmd.Parameters.AddWithValue("cap", request.RequestedEndpointCap);
        cmd.Parameters.AddWithValue("notes", (object?)request.Notes ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    public async Task<IReadOnlyList<Application>> ListApplicationsAsync()
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT id, company_name, contact_name, contact_email, requested_endpoint_cap,
                notes, status::text, created_organization_id, created_at
              FROM applications
              ORDER BY created_at DESC",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Application>();

        while (await reader.ReadAsync())
        {
            results.Add(new Application(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetGuid(7),
                reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return results;
    }

    public Task MarkInReviewAsync(Guid applicationId, Guid? reviewerUserId)
        => TransitionAsync(applicationId, new[] { "PENDING" }, "IN_REVIEW", reviewerUserId);

    public Task MarkInDiscussionAsync(Guid applicationId)
        => TransitionAsync(applicationId, new[] { "IN_REVIEW" }, "IN_DISCUSSION");

    public Task RejectApplicationAsync(Guid applicationId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.");

        // Only open applications can be rejected; an APPROVED one already has an org.
        return TransitionAsync(applicationId, OpenStatuses, "REJECTED", null, reason.Trim());
    }

    public async Task<ApproveApplicationResult> ApproveApplicationAsync(Guid applicationId)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        var app = await GetAsync(conn, applicationId);
        if (app.Status != "IN_DISCUSSION")
        {
            throw new InvalidOperationException(
                $"Application {applicationId} is {app.Status}, not IN_DISCUSSION - cannot approve.");
        }

        // Reuses OrganizationsService as-is: create org + first CSO + license, atomically.
        var orgResult = await _organizations.CreateOrganizationAsync(
            new CreateOrganizationRequest(app.CompanyName, app.ContactEmail, app.RequestedEndpointCap));

        await using var cmd = new NpgsqlCommand(
            @"UPDATE applications
              SET status = 'APPROVED', created_organization_id = @orgId, updated_at = now()
              WHERE id = @id",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);
        cmd.Parameters.AddWithValue("orgId", orgResult.OrganizationId);
        await cmd.ExecuteNonQueryAsync();

        return new ApproveApplicationResult(
            orgResult.OrganizationId,
            orgResult.CsoUserId,
            orgResult.LicenseId,
            orgResult.CsoTemporaryPassword);
    }

    private static async Task<(string Status, string CompanyName, string ContactEmail, int RequestedEndpointCap)>
        GetAsync(NpgsqlConnection conn, Guid applicationId)
    {
        await using var cmd = new NpgsqlCommand(
            @"SELECT status::text, company_name, contact_email, requested_endpoint_cap
              FROM applications
              WHERE id = @id",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException($"Application {applicationId} does not exist.");

        return (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
    }

    // The status check lives in the UPDATE's WHERE clause, so check-and-change is one
    // atomic statement (no read-then-write race between two reviewers).
    private async Task TransitionAsync(
        Guid applicationId,
        string[] allowedCurrentStatuses,
        string newStatus,
        Guid? reviewerUserId = null,
        string? rejectionReason = null)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"UPDATE applications
              SET status = @status::application_status,
                  reviewed_by = COALESCE(@reviewer::uuid, reviewed_by),
                  rejection_reason = COALESCE(@reason::text, rejection_reason),
                  updated_at = now()
              WHERE id = @id AND status::text = ANY(@allowed)",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);
        cmd.Parameters.AddWithValue("status", newStatus);
        cmd.Parameters.AddWithValue("reviewer", (object?)reviewerUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reason", (object?)rejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("allowed", allowedCurrentStatuses);

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
        {
            throw new InvalidOperationException(
                $"Application {applicationId} does not exist or is not in a state " +
                $"({string.Join("/", allowedCurrentStatuses)}) that allows moving to {newStatus}.");
        }
    }
}
