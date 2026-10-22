using Microsoft.Extensions.Hosting.WindowsServices;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "Sentinel Agent");
builder.Services.AddSingleton<EndpointIdentityStore>();
builder.Services.AddSingleton<AgentApiClient>();
builder.Services.AddSingleton<BoundedEventBuffer>();
builder.Services.AddSingleton<WindowsEventCollector>();
builder.Services.AddSingleton<AgentCommandExecutor>();
builder.Services.AddSingleton<NetworkIsolation>();
builder.Services.AddHostedService<AgentWorker>();
await builder.Build().RunAsync();

public sealed class AgentWorker(
    ILogger<AgentWorker> logger,
    IConfiguration configuration,
    AgentApiClient api,
    BoundedEventBuffer buffer,
    WindowsEventCollector collector,
    AgentCommandExecutor commands) : BackgroundService
{
    private AgentPolicy _policy = new(1, true, true, true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fingerprint = configuration["Sentinel:CertificateFingerprint"];
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            logger.LogCritical("Sentinel:CertificateFingerprint is required after enrollment; telemetry is disabled.");
            return;
        }

        var agentVersion = configuration["Sentinel:AgentVersion"] ?? "0.1.0";
        var heartbeatDue = DateTimeOffset.MinValue;
        var policyDue = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                if (now >= policyDue)
                {
                    _policy = await api.GetPolicyAsync(fingerprint, stoppingToken) ?? _policy;
                    policyDue = now.AddMinutes(5);
                }
                var command = await api.GetNextCommandAsync(fingerprint, stoppingToken);
                if (command is not null)
                {
                    var result = await commands.ExecuteAsync(command, stoppingToken);
                    using var report = await api.ReportCommandResultAsync(fingerprint, command.Id, result, stoppingToken);
                    report.EnsureSuccessStatusCode();
                }
                var events = collector.Collect(_policy);
                if (events.Count > 0) await buffer.EnqueueAsync(new TelemetryBatch("1.0", Guid.NewGuid(), events), stoppingToken);
                await FlushAsync(fingerprint, stoppingToken);
                if (now >= heartbeatDue)
                {
                    var bufferedBatches = await buffer.CountAsync(stoppingToken);
                    var status = bufferedBatches == 0 ? "HEALTHY" : "DEGRADED";
                    using var response = await api.SendHeartbeatAsync(fingerprint, new { agentVersion, policyVersion = _policy.Version, status, details = new Dictionary<string, string> { ["bufferedBatches"] = bufferedBatches.ToString() } }, stoppingToken);
                    response.EnsureSuccessStatusCode();
                    heartbeatDue = now.AddSeconds(60);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // The batch remains encrypted locally and is retried on the next tick.
                logger.LogWarning(ex, "Telemetry delivery cycle failed; retained events will be retried.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task FlushAsync(string fingerprint, CancellationToken cancellationToken)
    {
        for (var sent = 0; sent < 10; sent++)
        {
            var batch = await buffer.PeekAsync(cancellationToken);
            if (batch is null) return;
            using var response = await api.SendTelemetryAsync(fingerprint, batch, cancellationToken);
            if (!response.IsSuccessStatusCode) return;
            await buffer.RemoveFirstAsync(batch.BatchId, cancellationToken);
        }
    }
}
