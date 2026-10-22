using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Identity;

public interface IAuditService
{
    Task RecordAsync(Guid? organizationId, Guid? actorUserId, string action, string entityType, Guid? entityId, string outcome);
}

public sealed class AuditService(ISentinelDataSource dataSource) : IAuditService
{
    public async Task RecordAsync(Guid? organizationId, Guid? actorUserId, string action, string entityType, Guid? entityId, string outcome)
    {
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"INSERT INTO audit_logs (organization_id, actor_user_id, action, entity_type, entity_id, details)
VALUES (@org, @actor, @action, @type, @id, jsonb_build_object('outcome', @outcome))", conn);
        cmd.Parameters.AddWithValue("org", (object?)organizationId ?? DBNull.Value); cmd.Parameters.AddWithValue("actor", (object?)actorUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("action", action); cmd.Parameters.AddWithValue("type", entityType); cmd.Parameters.AddWithValue("id", (object?)entityId ?? DBNull.Value); cmd.Parameters.AddWithValue("outcome", outcome);
        await cmd.ExecuteNonQueryAsync();
    }
}
