// backend/src/Sentinel.Identity/IOrganizationContext.cs
using Microsoft.AspNetCore.Http;

namespace Sentinel.Identity;

public interface IOrganizationContext
{
    Guid? OrganizationId { get; }
    string Role { get; }
    bool IsPlatformTier { get; }
}

public class OrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId { get; }
    public string Role { get; }
    public bool IsPlatformTier => Role is "owner" or "support-team";

    public OrganizationContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HTTP context available.");

        Role = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Token contains no role claim.");

        var orgClaim = user.FindFirst("organizationId")?.Value;
        OrganizationId = string.IsNullOrEmpty(orgClaim) ? null : Guid.Parse(orgClaim);
    }
}