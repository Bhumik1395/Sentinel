using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public sealed record SigmaRuleImportItem(string RuleKey, string Name, string Severity, string? MitreTechnique, bool Enabled, JsonElement Definition);
public sealed record SigmaRuleImportRequest(IReadOnlyList<SigmaRuleImportItem> Items);

[ApiController]
[Route("detection-rules/sigma")]
[Authorize(Policy = "OwnerOnly")]
public sealed class DetectionRulesController(ISentinelDataSource dataSource, IOrganizationContext organization, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id,rule_key,name,severity::text,mitre_technique,enabled,definition,updated_at FROM detection_rules WHERE source='SIGMA' ORDER BY rule_key", connection);
        var items = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new { id = reader.GetGuid(0), ruleKey = reader.GetString(1), name = reader.GetString(2), severity = reader.GetString(3), mitreTechnique = reader.IsDBNull(4) ? null : reader.GetString(4), enabled = reader.GetBoolean(5), definition = JsonDocument.Parse(reader.GetString(6)).RootElement.Clone(), updatedAt = reader.GetFieldValue<DateTimeOffset>(7) });
        return Ok(new { items });
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] SigmaRuleImportRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count is 0 or > 500) return BadRequest(new { error = "Import must contain 1-500 Sigma rules." });
        var errors = new List<object>();
        foreach (var item in request.Items)
            if (!SigmaRuleValidation.TryValidate(item.RuleKey, item.Name, item.Severity, item.MitreTechnique, item.Definition, out var error)) errors.Add(new { item.RuleKey, error });
        if (errors.Count > 0) return BadRequest(new { error = "Import contains invalid rules; no changes were made.", items = errors });

        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var item in request.Items)
        {
            await using var command = new NpgsqlCommand(@"INSERT INTO detection_rules (rule_key,name,source,severity,mitre_technique,enabled,definition)
VALUES (@key,@name,'SIGMA',@severity::severity_level,@mitre,@enabled,@definition::jsonb)
ON CONFLICT (rule_key) DO UPDATE SET name=excluded.name,severity=excluded.severity,mitre_technique=excluded.mitre_technique,enabled=excluded.enabled,definition=excluded.definition", connection, transaction);
            command.Parameters.AddWithValue("key", item.RuleKey); command.Parameters.AddWithValue("name", item.Name); command.Parameters.AddWithValue("severity", item.Severity); command.Parameters.AddWithValue("mitre", (object?)item.MitreTechnique ?? DBNull.Value); command.Parameters.AddWithValue("enabled", item.Enabled); command.Parameters.AddWithValue("definition", item.Definition.GetRawText());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        await audit.RecordAsync(null, organization.UserId, "sigma_rule.imported", "detection_rule", null, $"imported:{request.Items.Count}");
        return Ok(new { imported = request.Items.Count });
    }
}
