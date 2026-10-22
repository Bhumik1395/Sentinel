using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity.Data;
using System.Text.Json;

namespace Sentinel.Api.Controllers;

public record AgentHeartbeat(string AgentVersion, int PolicyVersion, string Status, Dictionary<string, string>? Details);

[ApiController]
[Route("agent")]
[AllowAnonymous]
public sealed class AgentController(IAgentCertificateValidator certificates, ISentinelDataSource dataSource) : ControllerBase
{
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeat heartbeat)
    {
        if (heartbeat is null || heartbeat.AgentVersion.Length > 50 || heartbeat.PolicyVersion < 1 || heartbeat.Status is not ("HEALTHY" or "DEGRADED" or "OFFLINE")) return BadRequest(new { error = "Invalid heartbeat." });
        var endpointId = await certificates.ValidateAsync(HttpContext); if (endpointId is null) return Unauthorized();
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        await using var cmd = new NpgsqlCommand("UPDATE endpoints SET last_heartbeat_at=now(), agent_version=@version, policy_version=@policy, status=@status::endpoint_status WHERE id=@id RETURNING organization_id", conn, transaction);
        cmd.Parameters.AddWithValue("version", heartbeat.AgentVersion); cmd.Parameters.AddWithValue("policy", heartbeat.PolicyVersion); cmd.Parameters.AddWithValue("status", heartbeat.Status); cmd.Parameters.AddWithValue("id", endpointId.Value);
        if (await cmd.ExecuteScalarAsync() is not Guid organizationId) return Unauthorized();
        await using var history = new NpgsqlCommand("INSERT INTO endpoint_heartbeats (endpoint_id,organization_id,agent_version,policy_version,status,details) VALUES (@endpoint,@org,@version,@policy,@status::endpoint_status,@details::jsonb)", conn, transaction);
        history.Parameters.AddWithValue("endpoint", endpointId.Value); history.Parameters.AddWithValue("org", organizationId); history.Parameters.AddWithValue("version", heartbeat.AgentVersion); history.Parameters.AddWithValue("policy", heartbeat.PolicyVersion); history.Parameters.AddWithValue("status", heartbeat.Status); history.Parameters.AddWithValue("details", JsonSerializer.Serialize(heartbeat.Details ?? []));
        await history.ExecuteNonQueryAsync(); await transaction.CommitAsync(); return NoContent();
    }

    [HttpGet("policy")]
    public async Task<IActionResult> Policy()
    {
        var endpointId = await certificates.ValidateAsync(HttpContext); if (endpointId is null) return Unauthorized();
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"SELECT p.version,p.collect_process_events,p.collect_network_events,p.collect_script_events,p.collect_file_events,p.collect_registry_events
FROM endpoints e LEFT JOIN policies p ON p.id=e.policy_id WHERE e.id=@endpoint", conn);
        cmd.Parameters.AddWithValue("endpoint", endpointId.Value);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return Unauthorized();
        return Ok(reader.IsDBNull(0)
            ? new { version = 1, collectProcessEvents = true, collectNetworkEvents = true, collectScriptEvents = true, collectFileEvents = false, collectRegistryEvents = false }
            : new { version = reader.GetInt32(0), collectProcessEvents = reader.GetBoolean(1), collectNetworkEvents = reader.GetBoolean(2), collectScriptEvents = reader.GetBoolean(3), collectFileEvents = reader.GetBoolean(4), collectRegistryEvents = reader.GetBoolean(5) });
    }
}
