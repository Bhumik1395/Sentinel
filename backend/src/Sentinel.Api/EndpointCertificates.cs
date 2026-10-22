using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api;

public interface IEndpointCertificateService
{
    Task<IssuedEndpointCertificate> IssueAsync(Guid organizationId, Guid endpointId);
}

public record IssuedEndpointCertificate(string PfxBase64, string Password, string Fingerprint, DateTimeOffset ExpiresAt);

public sealed class EndpointCertificateService(IConfiguration configuration, ISentinelDataSource dataSource) : IEndpointCertificateService
{
    public async Task<IssuedEndpointCertificate> IssueAsync(Guid organizationId, Guid endpointId)
    {
        var pfx = configuration["CertificateAuthority:IssuingPfxBase64"]
            ?? throw new InvalidOperationException("CertificateAuthority:IssuingPfxBase64 is required.");
        var password = configuration["CertificateAuthority:IssuingPfxPassword"]
            ?? throw new InvalidOperationException("CertificateAuthority:IssuingPfxPassword is required.");
        using var issuer = new X509Certificate2(Convert.FromBase64String(pfx), password,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        if (!issuer.HasPrivateKey || !issuer.Extensions.OfType<X509BasicConstraintsExtension>().Any(x => x.CertificateAuthority))
            throw new InvalidOperationException("Configured issuing certificate is not a CA with a private key.");

        using var key = RSA.Create(3072);
        var request = new CertificateRequest($"CN=sentinel-endpoint-{endpointId}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2")], true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(new X509Extension(new Oid("1.3.6.1.4.1.55555.1.1"), System.Text.Encoding.UTF8.GetBytes($"{organizationId}:{endpointId}"), false));
        var now = DateTimeOffset.UtcNow;
        using var issued = request.Create(issuer, now.AddMinutes(-5), now.AddDays(30), RandomNumberGenerator.GetBytes(16));
        using var withKey = issued.CopyWithPrivateKey(key);
        var clientPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var thumbprint = withKey.Thumbprint;
        await using var conn = dataSource.CreateConnection(); await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE endpoints SET certificate_fingerprint=@fingerprint WHERE id=@id AND organization_id=@org", conn);
        cmd.Parameters.AddWithValue("fingerprint", thumbprint); cmd.Parameters.AddWithValue("id", endpointId); cmd.Parameters.AddWithValue("org", organizationId);
        if (await cmd.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Endpoint was not found for certificate issuance.");
        return new(Convert.ToBase64String(withKey.Export(X509ContentType.Pkcs12, clientPassword)), clientPassword, thumbprint, now.AddDays(30));
    }
}
