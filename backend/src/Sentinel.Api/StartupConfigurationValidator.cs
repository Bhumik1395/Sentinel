using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Sentinel.Api;

public static class StartupConfigurationValidator
{
    private static readonly string[] PlaceholderFragments =
    [
        "change-this", "replace-with", "your-", "<", "example", "placeholder"
    ];

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var failures = new List<string>();
        var connectionString = configuration.GetConnectionString("Sentinel");
        Require("ConnectionStrings:Sentinel", connectionString, failures);
        Require("Keycloak:Authority", configuration["Keycloak:Authority"], failures);
        Require("Keycloak:Audience", configuration["Keycloak:Audience"], failures);
        Require("Keycloak:AdminClientSecret", configuration["Keycloak:AdminClientSecret"], failures);
        Require("Bootstrap:OwnerPassword", configuration["Bootstrap:OwnerPassword"], failures);
        Require("CertificateAuthority:IssuingPfxBase64", configuration["CertificateAuthority:IssuingPfxBase64"], failures);
        Require("CertificateAuthority:IssuingPfxPassword", configuration["CertificateAuthority:IssuingPfxPassword"], failures);
        Require("CommandSigning:ActiveSecret", configuration["CommandSigning:ActiveSecret"], failures);

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            failures.Add("AllowedHosts must name one or more production hosts.");
        }

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(origin => origin == "*" || origin.Contains('*')))
        {
            failures.Add("Cors:AllowedOrigins must be an explicit, non-wildcard list.");
        }

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.TryGetValue("Trust Server Certificate", out var trustServerCertificate) &&
                bool.TryParse(trustServerCertificate?.ToString(), out var trustsServerCertificate) &&
                trustsServerCertificate)
            {
                failures.Add("ConnectionStrings:Sentinel must not set Trust Server Certificate=true outside Development.");
            }
        }

        var trustedProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        if (trustedProxies.Length == 0)
        {
            failures.Add("ForwardedHeaders:KnownProxies is required outside Development.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Unsafe production configuration: " + string.Join(" ", failures));
        }
    }

    private static void Require(string key, string? value, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            PlaceholderFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add($"{key} is missing or contains a placeholder.");
        }
    }
}
