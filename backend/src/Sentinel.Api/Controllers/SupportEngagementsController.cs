using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Identity.SupportEngagements;
using Sentinel.Identity;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("support-engagements")]
[Authorize(Policy = "SentinelCompany")]
public class SupportEngagementsController : ControllerBase
{
    private readonly ISupportEngagementService _engagements;
    private readonly IOrganizationContext _context;

    public SupportEngagementsController(ISupportEngagementService engagements, IOrganizationContext context) => (_engagements, _context) = (engagements, context);

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartEngagementRequest request)
    {
        if (_context.Role == "support-team" && (_context.UserId is null || request.SupportUserId != _context.UserId))
            return Forbid();
        var id = await _engagements.StartEngagementAsync(request);
        return Ok(new { engagementId = id });
    }

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id)
    {
        await _engagements.EndEngagementAsync(id);
        return NoContent();
}
}
