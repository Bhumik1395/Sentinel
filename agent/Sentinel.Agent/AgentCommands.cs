using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed record AgentCommand(Guid Id, string CommandType, JsonElement Payload, string Nonce, DateTimeOffset ExpiresAt, string Signature);
public sealed record CommandResult(string Nonce, bool Succeeded, object Result);

public sealed class AgentCommandExecutor(IConfiguration configuration, ILogger<AgentCommandExecutor> logger, NetworkIsolation isolation)
{
    public async Task<CommandResult> ExecuteAsync(AgentCommand command, CancellationToken cancellationToken)
    {
        if (command.ExpiresAt <= DateTimeOffset.UtcNow) return new(command.Nonce, false, new { error = "Command expired." });
        if (!Verify(command)) return new(command.Nonce, false, new { error = "Command signature rejected." });
        try
        {
            return command.CommandType switch
            {
                "KILL_PROCESS" => Kill(command),
                "ENDPOINT_ISOLATION" => new(command.Nonce, true, await isolation.IsolateAsync(cancellationToken)),
                _ => new(command.Nonce, false, new { error = "Unsupported command." })
            };
        }
        catch (Exception ex) { logger.LogError(ex, "Command {CommandId} failed", command.Id); return new(command.Nonce, false, new { error = "Command execution failed." }); }
    }

    private bool Verify(AgentCommand command)
    {
        var secret = configuration["Sentinel:CommandVerificationSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return false;
        var endpointId = configuration["Sentinel:EndpointId"];
        if (!Guid.TryParse(endpointId, out var endpoint)) return false;
        var value = string.Join('|', command.Id, endpoint, command.CommandType, command.Nonce, command.ExpiresAt.ToUniversalTime().ToString("O"), command.Payload.GetRawText());
        var calculated = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(calculated), Encoding.UTF8.GetBytes(command.Signature.ToLowerInvariant()));
    }

    private static CommandResult Kill(AgentCommand command)
    {
        if (!command.Payload.TryGetProperty("processId", out var value) || !value.TryGetInt32(out var processId) || processId <= 0) return new(command.Nonce, false, new { error = "processId is required." });
        using var process = Process.GetProcessById(processId); process.Kill(entireProcessTree: true); return new(command.Nonce, true, new { processId });
    }
}
