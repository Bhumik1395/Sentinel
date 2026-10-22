// backend/src/Sentinel.Identity/UserManagementService.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Sentinel.Identity.Data;
using Sentinel.Identity.Organizations;

namespace Sentinel.Identity;

public interface IUserManagementService
{
    Task<Guid> CreateUserAsync(Guid actingUserOrgId, string actingUserRole, string targetRole, string email);
    Task<Guid> InviteAsync(Guid organizationId, Guid actorId, string actorRole, string targetRole, string email, string displayName);
    Task SetActiveAsync(Guid organizationId, Guid userId, bool active);
}

public class UserManagementService : IUserManagementService
{
    private readonly IAuthorizationService _authz;
    private readonly ISentinelDataSource _dataSource;
    private readonly IKeycloakAdminProvisioningService _keycloak;

    public UserManagementService(IAuthorizationService authz, ISentinelDataSource dataSource, IKeycloakAdminProvisioningService keycloak) => (_authz, _dataSource, _keycloak) = (authz, dataSource, keycloak);

    public async Task<Guid> CreateUserAsync(
        Guid actingUserOrgId,
        string actingUserRole,
        string targetRole,
        string email)
    {
        var actingUser = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, actingUserRole)
            }));

        var result = await _authz.AuthorizeAsync(actingUser, targetRole, "CanManageUser");
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException(
                $"{actingUserRole} cannot create a {targetRole} account.");
        }

        return Guid.NewGuid();
    }

    public async Task<Guid> InviteAsync(Guid organizationId, Guid actorId, string actorRole, string targetRole, string email, string displayName)
    {
        var id = await CreateUserAsync(organizationId, actorRole, targetRole, email);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200) throw new ArgumentException("displayName is required.");
        var provisioned = await _keycloak.ProvisionUserWithPasswordAsync(email, targetRole, organizationId);
        try
        {
            await using var c = _dataSource.CreateConnection(); await c.OpenAsync();
            await using var q = new NpgsqlCommand("INSERT INTO users (id,organization_id,keycloak_user_id,email,display_name,role) VALUES (@id,@org,@keycloak,@email,@name,@role::user_role)", c);
            q.Parameters.AddWithValue("id", id); q.Parameters.AddWithValue("org", organizationId); q.Parameters.AddWithValue("keycloak", provisioned.KeycloakId.ToString()); q.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant()); q.Parameters.AddWithValue("name", displayName.Trim()); q.Parameters.AddWithValue("role", targetRole.ToUpperInvariant().Replace('-', '_'));
            await q.ExecuteNonQueryAsync(); return id;
        }
        catch { try { await _keycloak.DeleteUserAsync(provisioned.KeycloakId); } catch { } throw; }
    }

    public async Task SetActiveAsync(Guid organizationId, Guid userId, bool active)
    {
        await using var c = _dataSource.CreateConnection(); await c.OpenAsync();
        await using var get = new NpgsqlCommand("SELECT keycloak_user_id FROM users WHERE id=@id AND organization_id=@org", c); get.Parameters.AddWithValue("id", userId); get.Parameters.AddWithValue("org", organizationId);
        var value = await get.ExecuteScalarAsync() as string; if (!Guid.TryParse(value, out var keycloak)) throw new InvalidOperationException("User was not found.");
        await _keycloak.SetUserEnabledAsync(keycloak, active);
        await using var update = new NpgsqlCommand("UPDATE users SET status=@status::user_status WHERE id=@id", c); update.Parameters.AddWithValue("id", userId); update.Parameters.AddWithValue("status", active ? "ACTIVE" : "DEACTIVATED"); await update.ExecuteNonQueryAsync();
    }
}
