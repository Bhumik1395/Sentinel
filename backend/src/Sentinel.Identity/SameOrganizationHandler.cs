// backend/src/Sentinel.Identity/SameOrganizationHandler.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Sentinel.Identity;

public class SameOrganizationRequirement : IAuthorizationRequirement { }

public class SameOrganizationHandler : AuthorizationHandler<SameOrganizationRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOrganizationRequirement requirement,
        Guid resourceOrganizationId)
    {
        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role is "owner")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var orgClaim = context.User.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(orgClaim, out var callerOrgId) && callerOrgId == resourceOrganizationId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
