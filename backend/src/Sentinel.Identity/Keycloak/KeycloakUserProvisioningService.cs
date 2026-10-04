using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Sentinel.Identity.Organizations;

namespace Sentinel.Identity.Keycloak;

// Implements the interface and record that already live in Sentinel.Identity.Organizations
// (the earlier copy of this file redefined both, which could not compile alongside them).
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

    // No token caching: onboarding approval is a handful of calls a day.
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

    // Per-request auth header: mutating DefaultRequestHeaders is not safe to repeat.
    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _http.SendAsync(request);
    }

    private static string NewTemporaryPassword()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(15))
            .Replace('+', 'A')
            .Replace('/', 'b');

    public async Task<ProvisionedUser> ProvisionUserWithPasswordAsync(
        string email, string role, Guid organizationId)
    {
        var token = await GetAdminTokenAsync();
        var tempPassword = NewTemporaryPassword();

        // 1. Create the user. `organizationId` becomes a token claim via the protocol mapper.
        var create = await SendAsync(HttpMethod.Post, $"/admin/realms/{_realm}/users", token, new
        {
            username = email,
            email,
            enabled = true,
            attributes = new Dictionary<string, string[]>
            {
                ["organizationId"] = new[] { organizationId.ToString() }
            },
            credentials = new[]
            {
                new { type = "password", value = tempPassword, temporary = true }
            },
        });

        if (create.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException($"A Keycloak user for {email} already exists.");
        create.EnsureSuccessStatusCode();

        // Keycloak returns the new resource's URL in Location, not a body.
        var location = create.Headers.Location
            ?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user.");
        var keycloakId = Guid.Parse(location.Segments[^1]);

        // 2. Role mapping needs the role representation (id + name), not just the name.
        var roleResponse = await SendAsync(
            HttpMethod.Get, $"/admin/realms/{_realm}/roles/{Uri.EscapeDataString(role)}", token);
        roleResponse.EnsureSuccessStatusCode();
        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonElement>();

        // 3. Assign the realm role.
        var assign = await SendAsync(
            HttpMethod.Post,
            $"/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm",
            token,
            new[] { new { id = roleJson.GetProperty("id").GetString(), name = role } });
        assign.EnsureSuccessStatusCode();

        return new ProvisionedUser(keycloakId, tempPassword);
    }
}
