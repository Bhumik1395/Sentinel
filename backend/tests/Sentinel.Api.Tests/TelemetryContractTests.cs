using Sentinel.Api;
using Xunit;

namespace Sentinel.Api.Tests;

public sealed class TelemetryContractTests
{
    [Fact]
    public void Accepts_a_valid_v1_event()
    {
        var value = new TelemetryEvent("event-1", "POWERSHELL_EXECUTION", "AMSI", "HIGH", DateTimeOffset.UtcNow, null, null, "powershell.exe", "powershell -enc AAA", null, null);
        Assert.Null(TelemetryContract.Validate(value));
    }

    [Theory]
    [InlineData("NOT_A_REAL_TYPE", "AMSI", "HIGH")]
    [InlineData("PROCESS_START", "NOT_A_SOURCE", "HIGH")]
    [InlineData("PROCESS_START", "AMSI", "EMERGENCY")]
    public void Rejects_unknown_enum_values(string eventType, string source, string severity)
    {
        var value = new TelemetryEvent("event-1", eventType, source, severity, DateTimeOffset.UtcNow, null, null, null, null, null, null);
        Assert.NotNull(TelemetryContract.Validate(value));
    }
}
