using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Sentinel.Api;

public interface IDetectionService
{
    Task EvaluateAsync(NpgsqlConnection connection, Guid organizationId, Guid endpointId, Guid eventId, TelemetryEvent telemetryEvent, CancellationToken cancellationToken);
}

public sealed class DetectionService : IDetectionService
{
    public async Task EvaluateAsync(NpgsqlConnection connection, Guid organizationId, Guid endpointId, Guid eventId, TelemetryEvent item, CancellationToken cancellationToken)
    {
        await MatchIocsAsync(connection, organizationId, endpointId, eventId, item, cancellationToken);
        if (item.EventType == "POWERSHELL_EXECUTION" && ContainsEncodedPowerShell(item.CommandLine))
            await UpsertAlertAsync(connection, organizationId, endpointId, eventId, "behavior:encoded-powershell", "Encoded PowerShell execution", "HIGH", "BEHAVIORAL", "T1059.001", new { commandLine = item.CommandLine }, cancellationToken);
        await MatchSigmaRulesAsync(connection, organizationId, endpointId, eventId, item, cancellationToken);
    }

    private static bool ContainsEncodedPowerShell(string? commandLine) =>
        commandLine?.Contains("-enc", StringComparison.OrdinalIgnoreCase) == true ||
        commandLine?.Contains("encodedcommand", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task MatchIocsAsync(NpgsqlConnection connection, Guid org, Guid endpoint, Guid eventId, TelemetryEvent item, CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            (Type: "DOMAIN", Value: item.Domain), (Type: "IP_ADDRESS", Value: item.IpAddress), (Type: "FILE_HASH", Value: item.FileHash)
        }.Where(candidate => !string.IsNullOrWhiteSpace(candidate.Value));
        foreach (var candidate in candidates)
        {
            await using var command = new NpgsqlCommand("SELECT id, type, value FROM iocs WHERE organization_id=@org AND type=@type::ioc_type AND lower(value)=lower(@value) AND expires_at > now()", connection);
            command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("type", candidate.Type); command.Parameters.AddWithValue("value", candidate.Value!);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var iocId = reader.GetGuid(0);
                var type = reader.GetString(1);
                var value = reader.GetString(2);
                await reader.DisposeAsync();
                await UpsertAlertAsync(connection, org, endpoint, eventId, $"ioc:{iocId}", $"IOC {type} match", "HIGH", "IOC", null, new { type, value }, cancellationToken);
                break;
            }
        }
    }

    // A deliberately restricted Sigma MVP: rules contain { eventType, contains: { commandLine?, processName?, domain? } }.
    private static async Task MatchSigmaRulesAsync(NpgsqlConnection connection, Guid org, Guid endpoint, Guid eventId, TelemetryEvent item, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT id, rule_key, name, severity::text, mitre_technique, definition FROM detection_rules WHERE source='SIGMA' AND enabled", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rules = new List<(Guid Id, string Key, string Name, string Severity, string? Mitre, string Definition)>();
        while (await reader.ReadAsync(cancellationToken)) rules.Add((reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetString(5)));
        foreach (var rule in rules)
        {
            using var definition = JsonDocument.Parse(rule.Definition);
            var root = definition.RootElement;
            if (!root.TryGetProperty("eventType", out var eventType) || eventType.GetString() != item.EventType || !ContainsMatches(root, item)) continue;
            await UpsertAlertAsync(connection, org, endpoint, eventId, $"sigma:{rule.Id}", rule.Name, rule.Severity, "SIGMA", rule.Mitre, new { rule = rule.Key }, cancellationToken, rule.Id);
        }
    }

    private static bool ContainsMatches(JsonElement definition, TelemetryEvent item)
    {
        if (!definition.TryGetProperty("contains", out var contains) || contains.ValueKind != JsonValueKind.Object) return true;
        foreach (var property in contains.EnumerateObject())
        {
            var value = property.Value.GetString();
            var actual = property.Name switch { "commandLine" => item.CommandLine, "processName" => item.ProcessName, "domain" => item.Domain, _ => null };
            if (string.IsNullOrWhiteSpace(value) || actual?.Contains(value, StringComparison.OrdinalIgnoreCase) != true) return false;
        }
        return true;
    }

    private static async Task UpsertAlertAsync(NpgsqlConnection connection, Guid org, Guid endpoint, Guid eventId, string dedupeKey, string title, string severity, string source, string? mitre, object evidence, CancellationToken cancellationToken, Guid? ruleId = null)
    {
        await using var command = new NpgsqlCommand(@"INSERT INTO alerts (organization_id, endpoint_id, event_id, rule_id, title, severity, detection_source, mitre_technique, first_seen_at, last_seen_at, evidence, dedupe_key)
VALUES (@org,@endpoint,@event,@rule,@title,@severity::severity_level,@source,@mitre,now(),now(),@evidence::jsonb,@key)
ON CONFLICT (organization_id,dedupe_key) DO UPDATE SET occurrence_count=alerts.occurrence_count+1,last_seen_at=excluded.last_seen_at,event_id=excluded.event_id,evidence=excluded.evidence", connection);
        command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("endpoint", endpoint); command.Parameters.AddWithValue("event", eventId); command.Parameters.AddWithValue("rule", (object?)ruleId ?? DBNull.Value);
        command.Parameters.AddWithValue("title", title); command.Parameters.AddWithValue("severity", severity); command.Parameters.AddWithValue("source", source); command.Parameters.AddWithValue("mitre", (object?)mitre ?? DBNull.Value);
        command.Parameters.AddWithValue("evidence", JsonSerializer.Serialize(evidence)); command.Parameters.AddWithValue("key", dedupeKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
