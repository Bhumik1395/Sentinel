using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sentinel.Api;

public static partial class SigmaRuleValidation
{
    private static readonly HashSet<string> Fields = new(StringComparer.Ordinal) { "commandLine", "processName", "domain" };

    public static bool TryValidate(string? ruleKey, string? name, string? severity, string? mitreTechnique, JsonElement definition, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(ruleKey) || ruleKey.Length > 120 || !RuleKey().IsMatch(ruleKey)) { error = "ruleKey must use lowercase letters, digits, '.', '_' or '-'."; return false; }
        if (string.IsNullOrWhiteSpace(name) || name.Length > 240) { error = "name is required and must be at most 240 characters."; return false; }
        if (!TelemetryContract.Severities.Contains(severity ?? string.Empty)) { error = "severity is unsupported."; return false; }
        if (mitreTechnique?.Length > 40) { error = "mitreTechnique must be at most 40 characters."; return false; }
        if (definition.ValueKind != JsonValueKind.Object || !definition.TryGetProperty("eventType", out var eventType) || !TelemetryContract.EventTypes.Contains(eventType.GetString() ?? string.Empty)) { error = "definition.eventType must be a supported SUES event type."; return false; }
        if (!definition.TryGetProperty("contains", out var contains) || contains.ValueKind != JsonValueKind.Object || !contains.EnumerateObject().Any()) { error = "definition.contains must contain at least one supported field match."; return false; }
        foreach (var property in contains.EnumerateObject())
            if (!Fields.Contains(property.Name) || property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString()) || property.Value.GetString()!.Length > 512) { error = "definition.contains only supports non-empty commandLine, processName, or domain values up to 512 characters."; return false; }
        return true;
    }

    [GeneratedRegex("^[a-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex RuleKey();
}
