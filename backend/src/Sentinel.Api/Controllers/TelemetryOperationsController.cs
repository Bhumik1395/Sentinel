using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("operations/telemetry")]
[Authorize(Policy = "OwnerOnly")]
public sealed class TelemetryOperationsController(ISentinelDataSource dataSource) : ControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"SELECT
  (SELECT max(received_at) FROM telemetry_ingestion_batches),
  (SELECT count(*) FROM telemetry_ingestion_batches WHERE received_at > now() - interval '1 hour'),
  (SELECT count(*) FROM telemetry_rejections WHERE received_at > now() - interval '1 hour')", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var lastReceivedAt = reader.IsDBNull(0) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(0);
        var batchesLastHour = reader.GetInt64(1); var rejectedLastHour = reader.GetInt64(2);
        var status = lastReceivedAt is null || lastReceivedAt < DateTimeOffset.UtcNow.AddMinutes(-5) ? "DEGRADED" : "HEALTHY";
        return Ok(new { status, lastReceivedAt, batchesLastHour, rejectedLastHour });
    }
}
