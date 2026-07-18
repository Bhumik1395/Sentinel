using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Identity.Organizations;

public interface IOrganizationsService
{
    Task<CreateOrganizationResult> CreateOrganizationAsync(CreateOrganizationRequest request);

    Task<IReadOnlyList<Organization>> ListOrganizationsAsync();

    Task<Organization?> GetOrganizationAsync(Guid id);
}

public class OrganizationsService : IOrganizationsService
{
    private readonly ISentinelDataSource _dataSource;
    private readonly IKeycloakAdminProvisioningService _provisioning;

    public OrganizationsService(ISentinelDataSource dataSource, IKeycloakAdminProvisioningService provisioning)
    {
        _dataSource = dataSource;
        _provisioning = provisioning;
    }

    public async Task<CreateOrganizationResult> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        if (request.EndpointCap <= 0)
            throw new ArgumentException("endpointCap must be a positive integer.");

        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var orgId = Guid.NewGuid();
        var slug = Slugify(request.Name);
        await using (var cmd = new NpgsqlCommand(
            "INSERT INTO organizations (id, name, slug) VALUES (@id, @name, @slug)",
            conn,
            tx))
        {
            cmd.Parameters.AddWithValue("id", orgId);
            cmd.Parameters.AddWithValue("name", request.Name);
            cmd.Parameters.AddWithValue("slug", slug);
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Provision the first CSO (PRD section 6, TRD section 22 - Owner assigns first CSO, not
        // a Security Administrator; see the correction note at the top of this doc).
        var provisioned = await _provisioning.ProvisionUserWithPasswordAsync(request.CsoEmail, "cso", orgId);
        var csoKeycloakId = provisioned.KeycloakId;
        var csoUserId = Guid.NewGuid();
        await using (var cmd = new NpgsqlCommand(
            @"INSERT INTO users (id, organization_id, keycloak_user_id, email, display_name, role)
VALUES (@id, @orgId, @keycloakUserId, @email, @displayName, 'CSO')",
            conn,
            tx))
        {
            cmd.Parameters.AddWithValue("id", csoUserId);
            cmd.Parameters.AddWithValue("orgId", orgId);
            cmd.Parameters.AddWithValue("keycloakUserId", csoKeycloakId.ToString());
            cmd.Parameters.AddWithValue("email", request.CsoEmail);
            cmd.Parameters.AddWithValue("displayName", request.CsoEmail.Split('@')[0]);
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Assign the license (TRD section 34.3 - Owner-adjustable endpoint cap).
        var licenseId = Guid.NewGuid();
        await using (var cmd = new NpgsqlCommand(
            @"INSERT INTO licenses (id, organization_id, endpoint_cap)
VALUES (@id, @orgId, @cap)",
            conn,
            tx))
        {
            cmd.Parameters.AddWithValue("id", licenseId);
            cmd.Parameters.AddWithValue("orgId", orgId);
            cmd.Parameters.AddWithValue("cap", request.EndpointCap);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();

        return new CreateOrganizationResult(orgId, csoUserId, licenseId, provisioned.TemporaryPassword);
    }

    public async Task<IReadOnlyList<Organization>> ListOrganizationsAsync()
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, slug, created_at FROM organizations WHERE company_scope = false ORDER BY created_at DESC",
            conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<Organization>();
        while (await reader.ReadAsync())
        {
            results.Add(new Organization(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return results;
    }

    public async Task<Organization?> GetOrganizationAsync(Guid id)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, slug, created_at FROM organizations WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new Organization(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }

    private static string Slugify(string name)
    {
        var cleaned = new string(name
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray());

        var slug = string.Join(
            "-",
            cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return $"{(string.IsNullOrWhiteSpace(slug) ? "organization" : slug)}-{Guid.NewGuid():N}"[..Math.Min(
            (string.IsNullOrWhiteSpace(slug) ? "organization" : slug).Length + 7,
            120)];
    }
}
