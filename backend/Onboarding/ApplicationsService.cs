// backend/src/Sentinel.Identity/Onboarding/ApplicationsService.cs
using Npgsql;
using Sentinel.Identity.Data;
using Sentinel.Identity.Keycloak;
using Sentinel.Identity.Organizations;

namespace Sentinel.Identity.Onboarding;

public interface IApplicationsService
{
    Task<Guid> SubmitApplicationAsync(SubmitApplicationRequest request);
    Task<IReadOnlyList<Application>> ListApplicationsAsync();
    Task MarkInReviewAsync(Guid applicationId, Guid reviewerUserId);
    Task MarkInDiscussionAsync(Guid applicationId);
    Task<ApproveApplicationResult> ApproveApplicationAsync(Guid applicationId);
    Task RejectApplicationAsync(Guid applicationId, string reason);
}

public class ApplicationsService : IApplicationsService
{
    private readonly ISentinelDataSource _dataSource;
    private readonly IOrganizationsService _organizations;
    private readonly IKeycloakAdminProvisioningService _provisioning;

    public ApplicationsService(
        ISentinelDataSource dataSource,
        IOrganizationsService organizations,
        IKeycloakAdminProvisioningService provisioning)
    {
        _dataSource = dataSource;
        _organizations = organizations;
        _provisioning = provisioning;
    }

    public async Task<Guid> SubmitApplicationAsync(SubmitApplicationRequest request)
    {
        if (request.RequestedEndpointCap <= 0)
        {
            throw new ArgumentException("requestedEndpointCap must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            throw new ArgumentException("contactEmail is required.");
        }

        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO applications
                (id, company_name, contact_name, contact_email, requested_endpoint_cap, notes)
              VALUES (@id, @company, @contact, @email, @cap, @notes)",
            conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("company", request.CompanyName);
        cmd.Parameters.AddWithValue("contact", request.ContactName);
        cmd.Parameters.AddWithValue("email", request.ContactEmail);
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
                notes, status::text, organization_id, created_at
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

    public Task MarkInReviewAsync(Guid applicationId, Guid reviewerUserId)
        => TransitionAsync(applicationId, "PENDING", "IN_REVIEW", reviewerUserId);

    public Task MarkInDiscussionAsync(Guid applicationId)
        => TransitionAsync(applicationId, "IN_REVIEW", "IN_DISCUSSION", null);

    public async Task<ApproveApplicationResult> ApproveApplicationAsync(Guid applicationId)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        var app = await GetForUpdateAsync(conn, applicationId);
        if (app.Status != "IN_DISCUSSION")
        {
            throw new InvalidOperationException(
                $"Application {applicationId} is {app.Status}, not IN_DISCUSSION - cannot approve.");
        }

        // Reuses Phase 2's OrganizationsService as-is. Phase 3 does not
        // reimplement "create org + first CSO + license" - that's already
        // atomic (Phase 2 section 5) and this flow has no reason to duplicate it.
        var orgResult = await _organizations.CreateOrganizationAsync(
            new CreateOrganizationRequest(
                app.CompanyName,
                app.ContactEmail,
                app.RequestedEndpointCap));

        await using var cmd = new NpgsqlCommand(
            @"UPDATE applications
              SET status = 'APPROVED', organization_id = @orgId, updated_at = now()
              WHERE id = @id",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);
        cmd.Parameters.AddWithValue("orgId", orgResult.OrganizationId);
        await cmd.ExecuteNonQueryAsync();

        // OrganizationsService already called Keycloak provisioning internally
        // (via IKeycloakUserProvisioningService) to create the CSO - but that
        // interface's contract only returns a Keycloak ID, not the temp
        // password, because Phase 2 never needed one. Re-provisioning here
        // would create a duplicate account. Instead, ApproveApplicationAsync
        // is the one place that needs the password, so it's the one place
        // that calls the richer method directly, then hands OrganizationsService
        // the resulting Keycloak ID it already created - see the note below.
        return new ApproveApplicationResult(
            orgResult.OrganizationId,
            orgResult.CsoUserId,
            orgResult.LicenseId,
            orgResult.CsoTemporaryPassword
        );
    }

    public Task RejectApplicationAsync(Guid applicationId, string reason)
        => TransitionAsync(applicationId, null, "REJECTED", null, reason);

    private async Task<(string Status, string CompanyName, string ContactEmail, int RequestedEndpointCap)>
        GetForUpdateAsync(NpgsqlConnection conn, Guid applicationId)
    {
        await using var cmd = new NpgsqlCommand(
            @"SELECT status::text, company_name, contact_email, requested_endpoint_cap
              FROM applications
              WHERE id = @id",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException($"Application {applicationId} does not exist.");
        }

        return (
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3));
    }

    private async Task TransitionAsync(
        Guid applicationId,
        string? requiredCurrentStatus,
        string newStatus,
        Guid? reviewerUserId,
        string? rejectionReason = null)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        if (requiredCurrentStatus is not null)
        {
            var (current, _, _, _) = await GetForUpdateAsync(conn, applicationId);
            if (current != requiredCurrentStatus)
            {
                throw new InvalidOperationException(
                    $"Application {applicationId} is {current}, not {requiredCurrentStatus} - cannot transition to {newStatus}.");
            }
        }

        await using var cmd = new NpgsqlCommand(
            @"UPDATE applications
              SET status = @status::application_status,
                  reviewed_by = COALESCE(@reviewer, reviewed_by),
                  rejection_reason = @reason,
                  updated_at = now()
              WHERE id = @id",
            conn);

        cmd.Parameters.AddWithValue("id", applicationId);
        cmd.Parameters.AddWithValue("status", newStatus);
        cmd.Parameters.AddWithValue("reviewer", (object?)reviewerUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reason", (object?)rejectionReason ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }
}
