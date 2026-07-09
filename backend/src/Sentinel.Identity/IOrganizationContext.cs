using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Sentinel.Identity;

public interface IOrganizationContext
{
    Guid? OrganizationId { get; }
    Guid? UserId { get; }
    string Role { get; }
    bool IsPlatformTier { get; }
}

public class OrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId { get; }
    public Guid? UserId { get; }
    public string Role { get; }
    public bool IsPlatformTier => Role is "owner" or "support-team";

    public OrganizationContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

    // Allow anonymous requests (Swagger, health checks, etc.)
    if (user?.Identity?.IsAuthenticated != true)
    {
        Role = string.Empty;
        OrganizationId = null;
        UserId = null;
        return;
    }

    Role = user.Claims
        .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
        ?? throw new UnauthorizedAccessException("Authenticated token contains no role claim.");

    var orgClaim = user.FindFirst("organizationId")?.Value;
    OrganizationId = string.IsNullOrWhiteSpace(orgClaim)
        ? null
        : Guid.Parse(orgClaim);

    var subClaim = user.FindFirst("sub")?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    UserId = Guid.TryParse(subClaim, out var parsed)
        ? parsed
        : null;
    }
}   