using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Identity.SupportEngagements;

public interface ISupportEngagementService
{
    Task<Guid> StartEngagementAsync(StartEngagementRequest request);

    Task EndEngagementAsync(Guid engagementId);

    Task<bool> HasActiveEngagementAsync(Guid supportUserId, Guid organizationId);
}

public class SupportEngagementService : ISupportEngagementService
{
    private readonly ISentinelDataSource _dataSource;

    public SupportEngagementService(ISentinelDataSource dataSource) => _dataSource = dataSource;

    public async Task<Guid> StartEngagementAsync(StartEngagementRequest request)
    {
        if (request.DurationHours is <= 0 or > 72)
            throw new ArgumentException(
                "Engagement duration must be between 1 and 72 hours. " +
                "PRD/TRD do not specify a maximum - 72h is a TRD-unstated but " +
                "reasonable default guard; revisit if product specifies otherwise.");

        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();

        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO support_engagements
(id, organization_id, support_user_id, reason, expires_at)
VALUES (@id, @orgId, @userId, @reason, now() + (@hours || ' hours')::interval)",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("orgId", request.OrganizationId);
        cmd.Parameters.AddWithValue("userId", request.SupportUserId);
        cmd.Parameters.AddWithValue("reason", request.Reason);
        cmd.Parameters.AddWithValue("hours", request.DurationHours);

        await cmd.ExecuteNonQueryAsync();

        return id;
    }

    public async Task EndEngagementAsync(Guid engagementId)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE support_engagements SET status = 'ENDED', ended_at = now() WHERE id = @id AND status = 'ACTIVE'",
            conn);
        cmd.Parameters.AddWithValue("id", engagementId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasActiveEngagementAsync(Guid supportUserId, Guid organizationId)
    {
        await using var conn = _dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT 1 FROM support_engagements
WHERE support_user_id = @userId AND organization_id = @orgId
AND status = 'ACTIVE' AND expires_at > now()
LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("userId", supportUserId);
        cmd.Parameters.AddWithValue("orgId", organizationId);

        var result = await cmd.ExecuteScalarAsync();

        return result is not null;
    }
}
