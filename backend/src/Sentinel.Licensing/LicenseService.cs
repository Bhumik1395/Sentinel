using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Licensing;

public interface ILicenseService
{
    Task<License?> GetLicenseAsync(Guid organizationId);

    Task UpdateEndpointCapAsync(Guid organizationId, int newCap);

    Task<RegisterEndpointResult> RegisterEndpointAsync(RegisterEndpointRequest request);
}

public class LicenseService : ILicenseService
{
    private readonly ISentinelDataSource _dataSource;

    public LicenseService(ISentinelDataSource dataSource) => _dataSource = dataSource;

    public async Task<License?> GetLicenseAsync(Guid organizationId)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT id, organization_id, endpoint_cap, current_endpoint_count, status::text
FROM licenses WHERE organization_id = @orgId",
            conn);
        cmd.Parameters.AddWithValue("orgId", organizationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new License(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetString(4));
    }

    public async Task UpdateEndpointCapAsync(Guid organizationId, int newCap)
    {
        if (newCap <= 0)
            throw new ArgumentException("endpointCap must be a positive integer.");

        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        // Owner-adjustable per TRD section 34.3. Intentionally does not allow lowering the cap
        // below current_endpoint_count; the CHECK constraint from migration 002 would
        // reject that at the database layer anyway, but failing here with a clear message
        // is better than surfacing a raw Postgres constraint-violation error to the caller.
        var existing = await GetLicenseAsync(organizationId)
            ?? throw new InvalidOperationException("No license exists for this organization.");

        if (newCap < existing.CurrentEndpointCount)
            throw new InvalidOperationException(
                $"Cannot set endpoint cap to {newCap}: organization currently has " +
                $"{existing.CurrentEndpointCount} registered endpoints.");

        await using var cmd = new NpgsqlCommand(
            "UPDATE licenses SET endpoint_cap = @cap WHERE organization_id = @orgId",
            conn);
        cmd.Parameters.AddWithValue("cap", newCap);
        cmd.Parameters.AddWithValue("orgId", organizationId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<RegisterEndpointResult> RegisterEndpointAsync(RegisterEndpointRequest request)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Lock the license row for the duration of this transaction so two concurrent
        // registrations against the same organization can't both pass the cap check
        // before either one increments current_endpoint_count (classic TOCTOU race;
        // not called out in the PRD/TRD, but "endpoint registration beyond the cap is
        // rejected" per PRD section 24.2 implicitly requires this to hold under concurrency,
        // not just in the single-request case).
        await using var lockCmd = new NpgsqlCommand(
            @"SELECT endpoint_cap, current_endpoint_count, status::text
FROM licenses WHERE organization_id = @orgId FOR UPDATE",
            conn,
            tx);
        lockCmd.Parameters.AddWithValue("orgId", request.OrganizationId);

        await using var reader = await lockCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException("No license exists for this organization.");

        var cap = reader.GetInt32(0);
        var current = reader.GetInt32(1);
        var status = reader.GetString(2);
        await reader.CloseAsync();

        if (status == "SUSPENDED")
        {
            // Working assumption per Implementation Plan section 0, Open Decision #3:
            // suspension blocks new registrations only. It does not touch already
            // registered endpoints; there's no logic here that would, since this
            // method only ever runs for endpoints that don't exist yet.
            await tx.RollbackAsync();
            return new RegisterEndpointResult(RegistrationOutcome.LicenseSuspended, null);
        }

        if (current >= cap)
        {
            await tx.RollbackAsync();
            return new RegisterEndpointResult(RegistrationOutcome.CapReached, null);
        }

        var endpointId = Guid.NewGuid();
        await using (var insertCmd = new NpgsqlCommand(
            "INSERT INTO endpoints (id, organization_id, hostname) VALUES (@id, @orgId, @hostname)",
            conn,
            tx))
        {
            insertCmd.Parameters.AddWithValue("id", endpointId);
            insertCmd.Parameters.AddWithValue("orgId", request.OrganizationId);
            insertCmd.Parameters.AddWithValue("hostname", request.Hostname);
            await insertCmd.ExecuteNonQueryAsync();
        }

        await using (var updateCmd = new NpgsqlCommand(
            "UPDATE licenses SET current_endpoint_count = current_endpoint_count + 1 WHERE organization_id = @orgId",
            conn,
            tx))
        {
            updateCmd.Parameters.AddWithValue("orgId", request.OrganizationId);
            await updateCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();

        return new RegisterEndpointResult(RegistrationOutcome.Registered, endpointId);
    }
}
