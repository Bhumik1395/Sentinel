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

        // 1. Create the organization.
        var orgId = Guid.NewGuid();
        await using (var cmd = new NpgsqlCommand(
            "INSERT INTO organizations (id, type, name) VALUES (@id, 'CUSTOMER', @name)",
            conn,
            tx))
        {
            cmd.Parameters.AddWithValue("id", orgId);
            cmd.Parameters.AddWithValue("name", request.Name);
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Provision the first CSO (PRD section 6, TRD section 22 - Owner assigns first CSO, not
        // a Security Administrator; see the correction note at the top of this doc).
        var provisioned = await _provisioning.ProvisionUserWithPasswordAsync(request.CsoEmail, "cso", orgId);
        var csoKeycloakId = provisioned.KeycloakId;
        var csoUserId = Guid.NewGuid();
        await using (var cmd = new NpgsqlCommand(
            @"INSERT INTO users (id, keycloak_id, organization_id, role, email)
VALUES (@id, @keycloakId, @orgId, 'CSO', @email)",
            conn,
            tx))
        {
            cmd.Parameters.AddWithValue("id", csoUserId);
            cmd.Parameters.AddWithValue("keycloakId", csoKeycloakId);
            cmd.Parameters.AddWithValue("orgId", orgId);
            cmd.Parameters.AddWithValue("email", request.CsoEmail);
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
            "SELECT id, type, name, created_at FROM organizations WHERE type = 'CUSTOMER' ORDER BY created_at DESC",
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
            "SELECT id, type, name, created_at FROM organizations WHERE id = @id",
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
}
