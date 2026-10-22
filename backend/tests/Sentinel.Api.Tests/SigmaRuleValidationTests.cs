using System.Text.Json;
using Sentinel.Api;
using Xunit;

namespace Sentinel.Api.Tests;

public sealed class SigmaRuleValidationTests
{
    [Fact]
    public void Accepts_a_restricted_sigma_rule()
    {
        using var document = JsonDocument.Parse("""{ "eventType": "PROCESS_START", "contains": { "commandLine": "rundll32" } }""");
        Assert.True(SigmaRuleValidation.TryValidate("process.rundll32", "Rundll32 execution", "HIGH", "T1218.011", document.RootElement, out _));
    }

    [Fact]
    public void Rejects_unknown_match_fields()
    {
        using var document = JsonDocument.Parse("""{ "eventType": "PROCESS_START", "contains": { "raw": "anything" } }""");
        Assert.False(SigmaRuleValidation.TryValidate("bad-rule", "Bad", "HIGH", null, document.RootElement, out _));
    }
}
