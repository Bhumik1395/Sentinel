using System.Security.Cryptography.X509Certificates;

public sealed class EndpointIdentityStore
{
    private const string StoreName = "SentinelAgent";

    public X509Certificate2 ImportEnrollmentCertificate(string pfxBase64, string password, string expectedFingerprint)
    {
        var certificate = new X509Certificate2(Convert.FromBase64String(pfxBase64), password,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        if (!certificate.HasPrivateKey || !string.Equals(certificate.Thumbprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Enrollment certificate validation failed.");

        using var store = new X509Store(StoreName, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
        return certificate;
    }

    public X509Certificate2 GetActiveCertificate(string fingerprint)
    {
        using var store = new X509Store(StoreName, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates.Find(X509FindType.FindByThumbprint, fingerprint, validOnly: false)
            .OfType<X509Certificate2>().SingleOrDefault(c => c.HasPrivateKey && c.NotAfter > DateTime.UtcNow)
            ?? throw new InvalidOperationException("No valid Sentinel endpoint certificate is installed.");
    }

    public bool RequiresRotation(string fingerprint, TimeSpan threshold)
        => GetActiveCertificate(fingerprint).NotAfter.ToUniversalTime() <= DateTime.UtcNow.Add(threshold);
}
