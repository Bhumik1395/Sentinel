using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;

public sealed class AgentApiClient(EndpointIdentityStore store, IConfiguration configuration)
{
    public HttpClient Create(string fingerprint)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(store.GetActiveCertificate(fingerprint));
        return new HttpClient(handler) { BaseAddress = new Uri(configuration["Sentinel:ApiBaseUrl"] ?? throw new InvalidOperationException("Sentinel:ApiBaseUrl is required.")) };
    }

    public Task<HttpResponseMessage> SendHeartbeatAsync(string fingerprint, object heartbeat, CancellationToken cancellationToken)
        => Create(fingerprint).PostAsJsonAsync("agent/heartbeat", heartbeat, cancellationToken);

    // Telemetry is posted in bounded, idempotent batches.  Callers retain events locally
    // until this request succeeds, so transient API/network failures do not lose evidence.
    public Task<HttpResponseMessage> SendTelemetryAsync(string fingerprint, object batch, CancellationToken cancellationToken)
        => Create(fingerprint).PostAsJsonAsync("telemetry/ingest", batch, cancellationToken);

    public async Task<AgentPolicy?> GetPolicyAsync(string fingerprint, CancellationToken cancellationToken)
    {
        using var client = Create(fingerprint);
        return await client.GetFromJsonAsync<AgentPolicy>("agent/policy", cancellationToken);
    }

    public async Task<AgentCommand?> GetNextCommandAsync(string fingerprint, CancellationToken cancellationToken)
    {
        using var client = Create(fingerprint);
        using var response = await client.GetAsync("agent/commands/next", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentCommand>(cancellationToken: cancellationToken);
    }

    public Task<HttpResponseMessage> ReportCommandResultAsync(string fingerprint, Guid id, CommandResult result, CancellationToken cancellationToken)
        => Create(fingerprint).PostAsJsonAsync($"agent/commands/{id}/result", result, cancellationToken);
}
