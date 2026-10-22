using Sentinel.Identity;
using Sentinel.Identity.SupportEngagements;
using Sentinel.Identity.Data;
using Npgsql;
using System.Security.Claims;

namespace Sentinel.Api;

public class OrganizationIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationIsolationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IOrganizationContext orgContext,
        ISupportEngagementService engagements,
        ISentinelDataSource dataSource)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Claims are not trusted as tenant identity.  Resolve Keycloak's subject to
        // the local user record on every authenticated request and overwrite the
        // effective tenant/role claims before any controller or policy reads them.
        var subject = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        await using (var connection = dataSource.CreateConnection())
        {
            await connection.OpenAsync(context.RequestAborted);
            await using var command = new NpgsqlCommand("SELECT id,organization_id,role::text,status::text FROM users WHERE keycloak_user_id=@sub", connection);
            command.Parameters.AddWithValue("sub", subject);
            await using var reader = await command.ExecuteReaderAsync(context.RequestAborted);
            if (!await reader.ReadAsync(context.RequestAborted) || reader.GetString(3) != "ACTIVE")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            var identity = (ClaimsIdentity)context.User.Identity!;
            foreach (var claim in context.User.FindAll("sentinel_user_id").Concat(context.User.FindAll("organizationId")).ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new Claim("sentinel_user_id", reader.GetGuid(0).ToString()));
            if (!reader.IsDBNull(1)) identity.AddClaim(new Claim("organizationId", reader.GetGuid(1).ToString()));
            var role = reader.GetString(2).ToLowerInvariant().Replace('_', '-');
            foreach (var claim in context.User.FindAll(ClaimTypes.Role).ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        // Re-read the resolved context after claims have been normalized.
        orgContext = new OrganizationContext(context.RequestServices.GetRequiredService<IHttpContextAccessor>());
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
