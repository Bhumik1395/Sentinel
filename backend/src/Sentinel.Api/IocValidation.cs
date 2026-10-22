using System.Net;
using System.Text.RegularExpressions;

namespace Sentinel.Api;

public static partial class IocValidation
{
    public static bool TryNormalize(string? type, string? value, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value)) { error = "type and value are required."; return false; }
        var candidate = value.Trim();
        switch (type.ToUpperInvariant())
        {
            case "DOMAIN":
                candidate = candidate.TrimEnd('.').ToLowerInvariant();
                if (candidate.Length > 253 || !Uri.CheckHostName(candidate).Equals(UriHostNameType.Dns)) { error = "value is not a valid domain."; return false; }
                break;
            case "IP_ADDRESS":
                if (!IPAddress.TryParse(candidate, out var address)) { error = "value is not a valid IP address."; return false; }
                candidate = address.ToString();
                break;
            case "URL":
                if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) { error = "value is not a valid HTTP(S) URL."; return false; }
                candidate = uri.AbsoluteUri;
                break;
            case "FILE_HASH":
                candidate = candidate.ToLowerInvariant();
                if (candidate.Length is < 32 or > 128 || !Hex().IsMatch(candidate)) { error = "value must be a 32-128 character hexadecimal file hash."; return false; }
                break;
            default:
                error = "type is unsupported.";
                return false;
        }
        normalized = candidate;
        return true;
    }

    [GeneratedRegex("^[0-9a-f]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Hex();
}
