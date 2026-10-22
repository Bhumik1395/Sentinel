using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("telemetry")]
[AllowAnonymous]
public sealed class TelemetryController(IAgentCertificateValidator certificates, ISentinelDataSource dataSource, IDetectionService detection) : ControllerBase
{
    [HttpPost("ingest")]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Ingest([FromBody] TelemetryBatch batch, CancellationToken cancellationToken)
    {
        if (batch is null || batch.SchemaVersion != TelemetryContract.SchemaVersion || batch.BatchId == Guid.Empty)
            return BadRequest(new { error = "schemaVersion 1.0 and a non-empty batchId are required." });
        if (batch.Events is null || batch.Events.Count is 0 or > TelemetryContract.MaxBatchEvents)
            return BadRequest(new { error = "A batch must contain 1-500 events." });

        var endpointId = await certificates.ValidateAsync(HttpContext);
        if (endpointId is null) return Unauthorized();

        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var endpoint = await GetEndpointAsync(connection, endpointId.Value, cancellationToken);
        if (endpoint is null) return Unauthorized();
        var (organizationId, hostname) = endpoint.Value;
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(batch)))).ToLowerInvariant();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var previous = await BeginBatchAsync(connection, transaction, endpointId.Value, batch.BatchId, payloadHash, cancellationToken);
        if (previous is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (previous.Value.PayloadHash != payloadHash) return Conflict(new { error = "batchId was already used with a different payload." });
            return Ok(new { accepted = previous.Value.Accepted, duplicate = 0, rejected = previous.Value.Rejected, replayed = true });
        }

        var accepted = 0; var duplicate = 0;
        var rejections = new List<TelemetryRejection>();
        foreach (var item in batch.Events)
        {
            var error = TelemetryContract.Validate(item);
            if (error is not null)
            {
                rejections.Add(new(item.EventId, error));
                await RejectAsync(connection, transaction, endpointId.Value, batch.BatchId, item.EventId, error, cancellationToken);
                continue;
            }

            var eventId = await InsertEventAsync(connection, transaction, organizationId, endpointId.Value, hostname, item, cancellationToken);
            if (eventId is null) { duplicate++; continue; }
            accepted++;
            await detection.EvaluateAsync(connection, organizationId, endpointId.Value, eventId.Value, item, cancellationToken);
        }
        await CompleteBatchAsync(connection, transaction, endpointId.Value, batch.BatchId, accepted, rejections.Count, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Accepted(new TelemetryIngestResult(accepted, duplicate, rejections.Count, rejections));
    }

    private static async Task<(Guid OrganizationId, string Hostname)?> GetEndpointAsync(NpgsqlConnection connection, Guid endpointId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT organization_id, hostname FROM endpoints WHERE id=@id AND status <> 'RETIRED'", connection);
        command.Parameters.AddWithValue("id", endpointId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? (reader.GetGuid(0), reader.GetString(1)) : null;
    }

    private static async Task<(string PayloadHash, int Accepted, int Rejected)?> BeginBatchAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid endpoint, Guid batch, string hash, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(@"INSERT INTO telemetry_ingestion_batches (endpoint_id,batch_id,payload_hash)
VALUES (@endpoint,@batch,@hash) ON CONFLICT (endpoint_id,batch_id) DO NOTHING RETURNING id", connection, transaction);
        command.Parameters.AddWithValue("endpoint", endpoint); command.Parameters.AddWithValue("batch", batch); command.Parameters.AddWithValue("hash", hash);
        if (await command.ExecuteScalarAsync(cancellationToken) is not null) return null;
        await using var existing = new NpgsqlCommand("SELECT payload_hash,accepted_count,rejected_count FROM telemetry_ingestion_batches WHERE endpoint_id=@endpoint AND batch_id=@batch", connection, transaction);
        existing.Parameters.AddWithValue("endpoint", endpoint); existing.Parameters.AddWithValue("batch", batch);
        await using var reader = await existing.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? (reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)) : null;
    }

    private static async Task<Guid?> InsertEventAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid org, Guid endpoint, string hostname, TelemetryEvent item, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(@"INSERT INTO events (event_id,organization_id,endpoint_id,event_type,source,hostname,severity,occurred_at,domain,ip_address,process_name,command_line,file_hash,raw)
VALUES (@id,@org,@endpoint,@type::sues_event_type,@source::telemetry_source,@hostname,@severity::severity_level,@occurred,@domain,NULLIF(@ip,'')::inet,@process,@command,@fileHash,@raw::jsonb)
ON CONFLICT (organization_id,event_id) DO NOTHING RETURNING id", connection, transaction);
        command.Parameters.AddWithValue("id", item.EventId); command.Parameters.AddWithValue("org", org); command.Parameters.AddWithValue("endpoint", endpoint); command.Parameters.AddWithValue("type", item.EventType); command.Parameters.AddWithValue("source", item.Source);
        command.Parameters.AddWithValue("hostname", hostname); command.Parameters.AddWithValue("severity", item.Severity); command.Parameters.AddWithValue("occurred", item.OccurredAt); command.Parameters.AddWithValue("domain", (object?)item.Domain ?? DBNull.Value);
        command.Parameters.AddWithValue("ip", item.IpAddress ?? ""); command.Parameters.AddWithValue("process", (object?)item.ProcessName ?? DBNull.Value); command.Parameters.AddWithValue("command", (object?)item.CommandLine ?? DBNull.Value);
        command.Parameters.AddWithValue("fileHash", (object?)item.FileHash ?? DBNull.Value);
        command.Parameters.AddWithValue("raw", item.Raw is { } raw ? raw.GetRawText() : "{}");
        return await command.ExecuteScalarAsync(cancellationToken) is Guid id ? id : null;
    }

    private static async Task RejectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid endpoint, Guid batch, string? eventId, string reason, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("INSERT INTO telemetry_rejections (endpoint_id,batch_id,event_id,reason) VALUES (@endpoint,@batch,@event,@reason)", connection, transaction);
        command.Parameters.AddWithValue("endpoint", endpoint); command.Parameters.AddWithValue("batch", batch); command.Parameters.AddWithValue("event", (object?)eventId ?? DBNull.Value); command.Parameters.AddWithValue("reason", reason);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CompleteBatchAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid endpoint, Guid batch, int accepted, int rejected, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("UPDATE telemetry_ingestion_batches SET accepted_count=@accepted,rejected_count=@rejected WHERE endpoint_id=@endpoint AND batch_id=@batch", connection, transaction);
        command.Parameters.AddWithValue("accepted", accepted); command.Parameters.AddWithValue("rejected", rejected); command.Parameters.AddWithValue("endpoint", endpoint); command.Parameters.AddWithValue("batch", batch);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
