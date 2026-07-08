// backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs
using Sentinel.Identity;

namespace Sentinel.Api;

public class OrganizationIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationIsolationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IOrganizationContext orgContext)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        if (!orgContext.IsPlatformTier && orgContext.OrganizationId is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("No organization scope on token.");
            return;
        }

        if (context.Request.RouteValues.TryGetValue("orgId", out var routeOrgIdRaw)
            && Guid.TryParse(routeOrgIdRaw?.ToString(), out var routeOrgId)
            && !orgContext.IsPlatformTier
            && routeOrgId != orgContext.OrganizationId)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Cross-organization access denied.");
            return;
        }

        await _next(context);
    }
}