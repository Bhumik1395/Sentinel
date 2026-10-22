using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public sealed record IocImportItem(string Type, string Value, string? Description, DateTimeOffset ExpiresAt);
public sealed record IocImportRequest(IReadOnlyList<IocImportItem> Items);

[ApiController]
[Route("iocs")]
public sealed class IocsController(ISentinelDataSource dataSource, IOrganizationContext organization, IAuditService audit) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AnyOrganizationRole")]
    public async Task<IActionResult> List([FromQuery] bool includeExpired = false, CancellationToken cancellationToken = default)
    {
        if (organization.OrganizationId is not Guid org) return Forbid();
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"SELECT id,type::text,value,description,expires_at,created_at FROM iocs
WHERE organization_id=@org AND (@includeExpired OR expires_at > now()) ORDER BY expires_at ASC", connection);
        command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("includeExpired", includeExpired);
        var items = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new { id = reader.GetGuid(0), type = reader.GetString(1), value = reader.GetString(2), description = reader.IsDBNull(3) ? null : reader.GetString(3), expiresAt = reader.GetFieldValue<DateTimeOffset>(4), createdAt = reader.GetFieldValue<DateTimeOffset>(5) });
        return Ok(new { items });
    }

    [HttpPost("import")]
    [Authorize(Roles = "security-administrator")]
    public async Task<IActionResult> Import([FromBody] IocImportRequest request, CancellationToken cancellationToken)
    {
        if (organization.OrganizationId is not Guid org) return Forbid();
        if (request.Items is null || request.Items.Count is 0 or > 1_000) return BadRequest(new { error = "Import must contain 1-1,000 indicators." });
        var valid = new List<(string Type, string Value, string? Description, DateTimeOffset ExpiresAt)>();
        var errors = new List<object>();
        foreach (var item in request.Items)
        {
            var expirationInvalid = item.ExpiresAt <= DateTimeOffset.UtcNow;
            var descriptionInvalid = item.Description?.Length > 2_000;
            var indicatorValid = IocValidation.TryNormalize(item.Type, item.Value, out var normalized, out var error);
            if (expirationInvalid || descriptionInvalid || !indicatorValid)
                errors.Add(new { item.Type, item.Value, error = expirationInvalid ? "expiresAt must be in the future." : descriptionInvalid ? "description must be at most 2,000 characters." : error });
            else valid.Add((item.Type.ToUpperInvariant(), normalized, item.Description?.Trim(), item.ExpiresAt));
        }
        if (errors.Count > 0) return BadRequest(new { error = "Import contains invalid indicators; no changes were made.", items = errors });

        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var item in valid)
        {
            await using var command = new NpgsqlCommand(@"INSERT INTO iocs (organization_id,type,value,description,expires_at,created_by)
VALUES (@org,@type::ioc_type,@value,@description,@expires,@actor)
ON CONFLICT (organization_id,type,value) DO UPDATE SET description=excluded.description,expires_at=excluded.expires_at", connection, transaction);
            command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("type", item.Type); command.Parameters.AddWithValue("value", item.Value); command.Parameters.AddWithValue("description", (object?)item.Description ?? DBNull.Value); command.Parameters.AddWithValue("expires", item.ExpiresAt); command.Parameters.AddWithValue("actor", (object?)organization.UserId ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        await audit.RecordAsync(org, organization.UserId, "ioc.imported", "ioc_dataset", null, $"imported:{valid.Count}");
        return Ok(new { imported = valid.Count });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "security-administrator")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (organization.OrganizationId is not Guid org) return Forbid();
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM iocs WHERE id=@id AND organization_id=@org", connection);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("org", org);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) return NotFound();
        await audit.RecordAsync(org, organization.UserId, "ioc.deleted", "ioc", id, "success");
        return NoContent();
    }
}
