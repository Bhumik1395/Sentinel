using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public sealed record AlertStatusChange(string Status);

[ApiController]
[Route("alerts")]
[Authorize(Policy = "AnyOrganizationRole")]
public sealed class AlertsController(ISentinelDataSource dataSource, IOrganizationContext organization, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? severity = null, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        if (organization.OrganizationId is not Guid org) return Forbid();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"SELECT a.id,a.title,a.severity::text,e.hostname,a.detection_source,a.mitre_technique,a.last_seen_at,a.status::text,a.occurrence_count
FROM alerts a JOIN endpoints e ON e.id=a.endpoint_id WHERE a.organization_id=@org
AND (@severity IS NULL OR a.severity::text=@severity) AND (@status IS NULL OR a.status::text=@status)
ORDER BY a.last_seen_at DESC LIMIT @limit OFFSET @offset", connection);
        command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("severity", (object?)severity?.ToUpperInvariant() ?? DBNull.Value); command.Parameters.AddWithValue("status", (object?)status?.ToUpperInvariant() ?? DBNull.Value);
        command.Parameters.AddWithValue("limit", pageSize); command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var results = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new { id = reader.GetGuid(0), name = reader.GetString(1), severity = reader.GetString(2), endpoint = reader.GetString(3), user = "—", detectionSource = reader.GetString(4), mitreTechnique = reader.IsDBNull(5) ? "—" : reader.GetString(5), timestamp = reader.GetFieldValue<DateTimeOffset>(6), status = reader.GetString(7), occurrenceCount = reader.GetInt32(8) });
        return Ok(new { items = results, page, pageSize });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] AlertStatusChange change, CancellationToken cancellationToken)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "ACKNOWLEDGED", "IN_INVESTIGATION", "RESOLVED", "FALSE_POSITIVE" };
        if (organization.OrganizationId is not Guid org) return Forbid();
        var requestedStatus = change.Status?.ToUpperInvariant();
        if (requestedStatus is null || !allowed.Contains(requestedStatus)) return BadRequest(new { error = "Unsupported alert status." });
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE alerts SET status=@status::alert_status WHERE id=@id AND organization_id=@org", connection);
        command.Parameters.AddWithValue("status", requestedStatus); command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("org", org);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) return NotFound();
        await audit.RecordAsync(org, organization.UserId, "alert.status_changed", "alert", id, requestedStatus);
        return NoContent();
    }
}
