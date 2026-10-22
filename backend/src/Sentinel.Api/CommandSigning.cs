using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sentinel.Api;

public interface ICommandSigningService
{
    string Sign(Guid commandId, Guid endpointId, string commandType, string nonce, DateTimeOffset expiresAt, JsonElement payload);
}

/// <summary>HMAC signatures bind a command to one endpoint, nonce, expiry and payload.</summary>
public sealed class CommandSigningService(IConfiguration configuration) : ICommandSigningService
{
    public string Sign(Guid commandId, Guid endpointId, string commandType, string nonce, DateTimeOffset expiresAt, JsonElement payload)
    {
        var secret = configuration["CommandSigning:ActiveSecret"]
            ?? throw new InvalidOperationException("CommandSigning:ActiveSecret is required.");
        var canonical = string.Join('|', commandId, endpointId, commandType, nonce,
            expiresAt.ToUniversalTime().ToString("O"), payload.GetRawText());
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
