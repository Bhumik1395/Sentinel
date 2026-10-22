using System.Text.Json;
using Sentinel.Api;
using Xunit;

namespace Sentinel.Api.Tests;

public sealed class TelemetryFixtureTests
{
    [Fact]
    public async Task Encoded_powershell_fixture_is_a_valid_agent_batch()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "encoded-powershell-batch.json");
        await using var stream = File.OpenRead(path);
        var fixture = await JsonSerializer.DeserializeAsync<TelemetryBatch>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(fixture);
        Assert.Equal(TelemetryContract.SchemaVersion, fixture.SchemaVersion);
        var telemetryEvent = Assert.Single(fixture.Events);
        Assert.Equal("simulation:encoded-powershell:001", telemetryEvent.EventId);
        Assert.Null(TelemetryContract.Validate(telemetryEvent));
        Assert.Contains("-EncodedCommand", telemetryEvent.CommandLine!, StringComparison.OrdinalIgnoreCase);
    }
}
