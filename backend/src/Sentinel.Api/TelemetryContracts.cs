using System.Text.Json;

namespace Sentinel.Api;

public sealed record TelemetryBatch(string SchemaVersion, Guid BatchId, IReadOnlyList<TelemetryEvent> Events);

public sealed record TelemetryEvent(
    string EventId,
    string EventType,
    string Source,
    string Severity,
    DateTimeOffset OccurredAt,
    string? Domain,
    string? IpAddress,
    string? ProcessName,
    string? CommandLine,
    string? FileHash,
    JsonElement? Raw);

public sealed record TelemetryIngestResult(int Accepted, int Duplicate, int Rejected, IReadOnlyList<TelemetryRejection> Rejections);
public sealed record TelemetryRejection(string? EventId, string Reason);

public static class TelemetryContract
{
    public const string SchemaVersion = "1.0";
    public const int MaxBatchEvents = 500;
    public static readonly HashSet<string> EventTypes = new(StringComparer.Ordinal)
    {
        "PROCESS_START", "PROCESS_STOP", "NETWORK_CONNECTION", "DNS_QUERY", "FILE_ACCESS", "FILE_MODIFICATION",
        "REGISTRY_MODIFICATION", "POWERSHELL_EXECUTION", "ALERT", "INCIDENT"
    };
    public static readonly HashSet<string> Sources = new(StringComparer.Ordinal)
        { "ETW", "AMSI", "SYSMON", "WINDOWS_EVENT_LOG", "SENTINEL" };
    public static readonly HashSet<string> Severities = new(StringComparer.Ordinal)
        { "INFORMATIONAL", "LOW", "MEDIUM", "HIGH", "CRITICAL" };

    public static string? Validate(TelemetryEvent value)
    {
        if (string.IsNullOrWhiteSpace(value.EventId) || value.EventId.Length > 120) return "eventId is required and must be at most 120 characters.";
        if (!EventTypes.Contains(value.EventType)) return "eventType is unsupported.";
        if (!Sources.Contains(value.Source)) return "source is unsupported.";
        if (!Severities.Contains(value.Severity)) return "severity is unsupported.";
        if (value.OccurredAt > DateTimeOffset.UtcNow.AddMinutes(5) || value.OccurredAt < DateTimeOffset.UtcNow.AddDays(-365)) return "occurredAt is outside the permitted range.";
        if (value.CommandLine?.Length > 8192 || value.ProcessName?.Length > 1024 || value.Domain?.Length > 253 || value.FileHash?.Length > 128) return "an event field exceeds its maximum length.";
        return null;
    }
}
