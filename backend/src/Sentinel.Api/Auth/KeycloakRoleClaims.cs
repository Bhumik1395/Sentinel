using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Api.Auth;

public static class KeycloakRoleClaims
{
    // Keycloak also puts default-roles-sentinel, offline_access and uma_authorization in
    // every token. OrganizationContext reads the FIRST role claim, so only Sentinel's
    // five roles may be added or it could pick the wrong one.
    private static readonly HashSet<string> SentinelRoles = new(StringComparer.Ordinal)
    {
        "owner",
        "support-team",
        "cso",
        "security-administrator",
        "security-analyst"
    };

    public static void Apply(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        var raw = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(raw))
            return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
                return;

            foreach (var role in roles.EnumerateArray())
            {
                var name = role.GetString();
                if (name is not null
                    && SentinelRoles.Contains(name)
                    && !identity.HasClaim(ClaimTypes.Role, name))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, name));
                }
            }
        }
        catch (JsonException)
        {
            // Malformed realm_access: treat as no roles. Authorization will deny.
        }
    }
}
