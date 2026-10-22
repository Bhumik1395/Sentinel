using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api;

// Retention is enforced in the service layer so each tenant's configured log_retention_days is honoured.
public sealed class TelemetryRetentionService(ISentinelDataSource dataSource, ILogger<TelemetryRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PurgeAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Telemetry retention purge failed."); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    internal async Task PurgeAsync(CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"DELETE FROM events e USING organizations o
WHERE e.organization_id=o.id AND e.ingested_at < now() - (o.log_retention_days * interval '1 day')", connection);
        var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
        if (deleted > 0) logger.LogInformation("Telemetry retention purged {Count} events.", deleted);
    }
}
