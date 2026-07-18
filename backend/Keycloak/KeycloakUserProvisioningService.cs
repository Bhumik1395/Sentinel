using System.Net.Http.Json;
using System.Text.Json;
using Sentinel.Identity.Organizations;

namespace Sentinel.Identity.Keycloak;

public record ProvisionedUser(Guid KeycloakId, string TemporaryPassword);

// Extends the Organizations-phase interface with the one extra bit of
// information the onboarding flow actually needs back: the temp password,
// since SMTP delivery is explicitly out of scope this phase (see doc section 0).
public interface IKeycloakAdminProvisioningService : IKeycloakUserProvisioningService
{
    Task<ProvisionedUser> ProvisionUserWithPasswordAsync(string email,
        string role, Guid organizationId);
}

public class KeycloakUserProvisioningService : IKeycloakAdminProvisioningService
{
    private readonly HttpClient _http;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeycloakUserProvisioningService(HttpClient http, IConfiguration config)
    {
        _http = http; // BaseAddress = Keycloak base URL, set in Program.cs
        _realm = config["Keycloak:Realm"] ?? "sentinel";
        _clientId = config["Keycloak:AdminClientId"] ?? "sentinel-api";
        _clientSecret = config["Keycloak:AdminClientSecret"]
            ?? throw new InvalidOperationException("Keycloak:AdminClientSecret is missing.");
    }

    // ponytail: no token caching. Onboarding approval is a handful of
    // calls a day, not a hot path - add caching if that stops being true.
    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _http.PostAsync(
            $"/realms/{_realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
            }));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("access_token").GetString()!;
    }

    public async Task<Guid> ProvisionUserAsync(string email, string role,
        Guid organizationId)
        => (await ProvisionUserWithPasswordAsync(email, role, organizationId)).KeycloakId;

    public async Task<ProvisionedUser> ProvisionUserWithPasswordAsync(
        string email, string role, Guid organizationId)
    {
        var adminToken = await GetAdminTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var tempPassword = Guid.NewGuid().ToString("N")[..16];

        // 1. Create the user.
        var createResponse = await _http.PostAsJsonAsync(
            $"/admin/realms/{_realm}/users", new
            {
                username = email,
                email,
                enabled = true,
                attributes = new { organizationId = new[] { organizationId.ToString() } },
                credentials = new[]
                {
                    new { type = "password", value = tempPassword, temporary = true }
                },
            });
        createResponse.EnsureSuccessStatusCode();

        // Keycloak returns the new resource's URL in Location, not a body.
        var location = createResponse.Headers.Location
            ?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user.");
        var keycloakId = Guid.Parse(location.Segments[^1]);

        // 2. Look up the realm role's ID (role-mapping-by-name requires the
        // role representation, not just its name).
        var roleResponse = await _http.GetFromJsonAsync<JsonElement>(
            $"/admin/realms/{_realm}/roles/{role}");

        // 3. Assign the realm role.
        var assignResponse = await _http.PostAsJsonAsync(
            $"/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm",
            new[] { new { id = roleResponse.GetProperty("id").GetString(), name = role } });
        assignResponse.EnsureSuccessStatusCode();

        return new ProvisionedUser(keycloakId, tempPassword);
    }
}
