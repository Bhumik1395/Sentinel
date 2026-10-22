using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentinel.Api;
using Xunit;

namespace Sentinel.Api.Tests;

public sealed class StartupConfigurationValidatorTests
{
    [Fact]
    public void Production_rejects_unsafe_defaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sentinel"] = "Host=db;Trust Server Certificate=true",
                ["Keycloak:Authority"] = "https://identity.example/realms/sentinel",
                ["Keycloak:Audience"] = "sentinel-api",
                ["Keycloak:AdminClientSecret"] = "change-this-secret",
                ["Bootstrap:OwnerPassword"] = "change-this-password",
                ["AllowedHosts"] = "*",
                ["Cors:AllowedOrigins:0"] = "*"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StartupConfigurationValidator.Validate(configuration, new TestEnvironment("Production")));

        Assert.Contains("Unsafe production configuration", exception.Message);
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Sentinel.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
