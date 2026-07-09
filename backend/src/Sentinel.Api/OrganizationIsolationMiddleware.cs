using Sentinel.Identity;
using Sentinel.Identity.SupportEngagements;

namespace Sentinel.Api;

public class OrganizationIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationIsolationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IOrganizationContext orgContext,
        ISupportEngagementService engagements)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        if (orgContext.Role == "owner")
        {
            // Owner genuinely bypasses org boundaries; unchanged from Phase 1.
            await _next(context);
            return;
        }

        Guid routeOrgId = Guid.Empty;

        var hasRouteOrgId =
            context.Request.RouteValues.TryGetValue("orgId", out var routeOrgIdRaw)
            && Guid.TryParse(routeOrgIdRaw?.ToString(), out routeOrgId);

        if (orgContext.Role == "support-team")
        {
            if (!hasRouteOrgId)
            {
                // Support Team routes that aren't scoped to a specific organization
                // (e.g. starting a new engagement, which takes the org ID in the body,
                // not the route) are allowed through here and rely on their own
                // controller-level [Authorize] policy instead.
                await _next(context);
                return;
            }

            // TODO(Phase 3+): once JWTs carry a stable internal user ID claim wired
            // to the `users` table, replace this placeholder lookup. For now this
            // assumes a `sub`-to-users.id mapping exists; flag to whoever wires
            // Keycloak protocol mappers in Phase 3 if that assumption doesn't hold.
            var supportUserId = orgContext.UserId 
                ?? throw new UnauthorizedAccessException("Token contains no resolvable user identity.");
            var active = await engagements.HasActiveEngagementAsync(supportUserId, routeOrgId);

            if (!active)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(
                    "No active support engagement for this organization.");
                return;
            }

            await _next(context);
            return;
        }

        // CSO / Security Administrator / Security Analyst: unchanged Phase 1 behavior.
        if (orgContext.OrganizationId is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("No organization scope on token.");
            return;
        }

        if (hasRouteOrgId && routeOrgId != orgContext.OrganizationId)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Cross-organization access denied.");
            return;
        }

        await _next(context);
    }
}
