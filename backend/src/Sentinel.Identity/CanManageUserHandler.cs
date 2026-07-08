// backend/src/Sentinel.Identity/CanManageUserHandler.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Sentinel.Identity;

public class CanManageUserRequirement : IAuthorizationRequirement { }

public class CanManageUserHandler : AuthorizationHandler<CanManageUserRequirement, string>
{
    private static readonly Dictionary<string, string[]> AllowedTargets = new()
    {
        ["owner"] = new[] { "cso" },
        ["cso"] = new[] { "security-administrator" },
        ["security-administrator"] = new[] { "security-analyst" }
    };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanManageUserRequirement requirement,
        string targetRole)
    {
        var actorRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (actorRole is not null
            && AllowedTargets.TryGetValue(actorRole, out var allowed)
            && allowed.Contains(targetRole))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
