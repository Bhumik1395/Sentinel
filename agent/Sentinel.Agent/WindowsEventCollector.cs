using System.Diagnostics.Eventing.Reader;
using System.Text.Json;

// Event Log is the common, supportable collection surface for the first MVP sources:
// Sysmon Operational (1/3/22), Security 4688 process creation, PowerShell 4104 script blocks, and AMSI Operational.
public sealed class WindowsEventCollector(ILogger<WindowsEventCollector> logger)
{
    private readonly Dictionary<string, long> _lastRecordByChannel = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<TelemetryEvent> Collect(AgentPolicy policy, int limit = 250)
    {
        var output = new List<TelemetryEvent>();
        if (policy.CollectProcessEvents || policy.CollectNetworkEvents) ReadChannel("Microsoft-Windows-Sysmon/Operational", policy, output, limit);
        if (policy.CollectProcessEvents) ReadChannel("Security", policy, output, limit);
        if (policy.CollectScriptEvents) ReadChannel("Microsoft-Windows-PowerShell/Operational", policy, output, limit);
        if (policy.CollectScriptEvents) ReadChannel("Microsoft-Windows-AMSI/Operational", policy, output, limit);
        return output;
    }

    private void ReadChannel(string channel, AgentPolicy policy, List<TelemetryEvent> output, int limit)
    {
        try
        {
            using var reader = new EventLogReader(new EventLogQuery(channel, PathType.LogName));
            while (output.Count < limit && reader.ReadEvent() is EventRecord record)
            {
                using (record)
                {
                    var recordId = record.RecordId ?? 0;
                    if (_lastRecordByChannel.TryGetValue(channel, out var previous) && recordId <= previous) continue;
                    _lastRecordByChannel[channel] = recordId;
                    var eventType = ResolveType(channel, record.Id, policy);
                    if (eventType is null) continue;
                    var message = SafeFormat(record);
                    var source = channel.StartsWith("Microsoft-Windows-Sysmon", StringComparison.Ordinal) ? "SYSMON" : channel == "Microsoft-Windows-AMSI/Operational" ? "AMSI" : "WINDOWS_EVENT_LOG";
                    output.Add(new TelemetryEvent($"{channel}:{recordId}", eventType, source, "INFORMATIONAL", record.TimeCreated ?? DateTimeOffset.UtcNow, Extract(message, "QueryName"), Extract(message, "DestinationIp"), Extract(message, "Image"), eventType == "POWERSHELL_EXECUTION" ? message : null, null, JsonSerializer.SerializeToElement(new { channel, eventId = record.Id, recordId, provider = record.ProviderName })));
                }
            }
        }
        catch (EventLogNotFoundException) { logger.LogDebug("Windows event channel {Channel} is not available.", channel); }
        catch (UnauthorizedAccessException) { logger.LogWarning("Sentinel Agent cannot read Windows event channel {Channel}.", channel); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not collect Windows event channel {Channel}.", channel); }
    }

    private static string? ResolveType(string channel, int id, AgentPolicy policy) => channel switch
    {
        "Microsoft-Windows-Sysmon/Operational" when id == 1 && policy.CollectProcessEvents => "PROCESS_START",
        "Microsoft-Windows-Sysmon/Operational" when id == 3 && policy.CollectNetworkEvents => "NETWORK_CONNECTION",
        "Microsoft-Windows-Sysmon/Operational" when id == 22 && policy.CollectNetworkEvents => "DNS_QUERY",
        "Security" when id == 4688 && policy.CollectProcessEvents => "PROCESS_START",
        "Microsoft-Windows-PowerShell/Operational" when id == 4104 && policy.CollectScriptEvents => "POWERSHELL_EXECUTION",
        "Microsoft-Windows-AMSI/Operational" when policy.CollectScriptEvents => "POWERSHELL_EXECUTION",
        _ => null
    };

    private static string SafeFormat(EventRecord record) { try { return record.FormatDescription() ?? string.Empty; } catch { return string.Empty; } }
    private static string? Extract(string text, string name) => text.Length <= 253 && text.Contains(name, StringComparison.OrdinalIgnoreCase) ? text : null;
}
