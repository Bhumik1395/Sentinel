using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Licensing;

public interface IEnrollmentTokenService
{
    Task<string> CreateAsync(CreateEnrollmentTokenRequest request);
    Task<Guid?> RedeemAsync(RedeemEnrollmentTokenRequest request);
}

public sealed class EnrollmentTokenService(ISentinelDataSource dataSource) : IEnrollmentTokenService
{
    public async Task<string> CreateAsync(CreateEnrollmentTokenRequest request)
    {
        if (request.ExpiresInMinutes is < 5 or > 1_440) throw new ArgumentException("expiresInMinutes must be between 5 and 1440.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"INSERT INTO enrollment_tokens (organization_id, token_hash, expires_at)
VALUES (@org, @hash, now() + (@minutes || ' minutes')::interval)", conn);
        cmd.Parameters.AddWithValue("org", request.OrganizationId); cmd.Parameters.AddWithValue("hash", Hash(token));
        cmd.Parameters.AddWithValue("minutes", request.ExpiresInMinutes);
        await cmd.ExecuteNonQueryAsync(); return token;
    }

    public async Task<Guid?> RedeemAsync(RedeemEnrollmentTokenRequest request)
    {
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync(); await using var tx = await conn.BeginTransactionAsync();
        await using var cmd = new NpgsqlCommand(@"UPDATE enrollment_tokens SET redeemed_at = now()
WHERE token_hash = @hash AND redeemed_at IS NULL AND expires_at > now() RETURNING organization_id", conn, tx);
        cmd.Parameters.AddWithValue("hash", Hash(request.Token));
        var result = await cmd.ExecuteScalarAsync(); await tx.CommitAsync();
        return result is Guid id ? id : null;
    }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
