// backend/src/Sentinel.Identity/UserManagementService.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Sentinel.Identity;

public interface IUserManagementService
{
    Task<Guid> CreateUserAsync(Guid actingUserOrgId, string actingUserRole, string targetRole, string email);
}

public class UserManagementService : IUserManagementService
{
    private readonly IAuthorizationService _authz;

    public UserManagementService(IAuthorizationService authz) => _authz = authz;

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
}
