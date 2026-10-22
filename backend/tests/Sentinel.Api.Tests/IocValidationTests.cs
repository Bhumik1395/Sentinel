using Sentinel.Api;
using Xunit;

namespace Sentinel.Api.Tests;

public sealed class IocValidationTests
{
    [Theory]
    [InlineData("DOMAIN", "Example.COM.", "example.com")]
    [InlineData("IP_ADDRESS", "2001:0db8::1", "2001:db8::1")]
    [InlineData("FILE_HASH", "AABBCCDDEEFF00112233445566778899", "aabbccddeeff00112233445566778899")]
    public void Normalizes_supported_indicators(string type, string value, string expected)
    {
        Assert.True(IocValidation.TryNormalize(type, value, out var normalized, out _));
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Rejects_a_non_http_url()
        => Assert.False(IocValidation.TryNormalize("URL", "file:///sensitive", out _, out _));
}
