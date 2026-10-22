using System.Security.Cryptography;
using System.Text.Json;

// DPAPI binds queued telemetry to this endpoint.  A batch is removed only after the API accepts it.
public sealed class BoundedEventBuffer
{
    private readonly string _path;
    private readonly int _maxBatches;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BoundedEventBuffer(IConfiguration configuration)
    {
        _path = configuration["Sentinel:EventBufferPath"] ?? Path.Combine(AppContext.BaseDirectory, "events.queue");
        _maxBatches = int.TryParse(configuration["Sentinel:EventBufferMaxBatches"], out var value) ? Math.Clamp(value, 10, 10_000) : 1_000;
    }

    public async Task EnqueueAsync(TelemetryBatch batch, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAsync(cancellationToken);
            items.Add(batch);
            if (items.Count > _maxBatches) items.RemoveRange(0, items.Count - _maxBatches); // oldest telemetry is evicted first.
            await WriteAsync(items, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<TelemetryBatch?> PeekAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).FirstOrDefault(); }
        finally { _gate.Release(); }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).Count; }
        finally { _gate.Release(); }
    }

    public async Task RemoveFirstAsync(Guid batchId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAsync(cancellationToken);
            if (items.FirstOrDefault()?.BatchId == batchId)
            {
                items.RemoveAt(0);
                await WriteAsync(items, cancellationToken);
            }
        }
        finally { _gate.Release(); }
    }

    private async Task<List<TelemetryBatch>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return [];
        var encrypted = await File.ReadAllBytesAsync(_path, cancellationToken);
        var json = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
        return JsonSerializer.Deserialize<List<TelemetryBatch>>(json) ?? [];
    }

    private async Task WriteAsync(List<TelemetryBatch> items, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(items);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
        await File.WriteAllBytesAsync(_path, encrypted, cancellationToken);
    }
}
