using System.Text.Json;

public sealed record AgentPolicy(int Version, bool CollectProcessEvents, bool CollectNetworkEvents, bool CollectScriptEvents, bool CollectFileEvents = false, bool CollectRegistryEvents = false);

public sealed record TelemetryBatch(string SchemaVersion, Guid BatchId, IReadOnlyList<TelemetryEvent> Events);

public sealed record TelemetryEvent(
    string EventId, string EventType, string Source, string Severity, DateTimeOffset OccurredAt,
    string? Domain, string? IpAddress, string? ProcessName, string? CommandLine, string? FileHash, JsonElement? Raw);
