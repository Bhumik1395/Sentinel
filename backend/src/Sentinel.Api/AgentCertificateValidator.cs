using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api;

public interface IAgentCertificateValidator { Task<Guid?> ValidateAsync(HttpContext context); }
public sealed class AgentCertificateValidator(ISentinelDataSource dataSource) : IAgentCertificateValidator
{
    public async Task<Guid?> ValidateAsync(HttpContext context)
    {
        var certificate = await context.Connection.GetClientCertificateAsync();
        if (certificate?.Thumbprint is not { Length: > 0 } fingerprint) return null;
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT id FROM endpoints WHERE certificate_fingerprint=@fingerprint AND status <> 'RETIRED'", conn);
        cmd.Parameters.AddWithValue("fingerprint", fingerprint);
        return await cmd.ExecuteScalarAsync() is Guid endpointId ? endpointId : null;
    }
}
