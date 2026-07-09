using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Identity.SupportEngagements;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("support-engagements")]
[Authorize(Policy = "SentinelCompany")]
public class SupportEngagementsController : ControllerBase
{
    private readonly ISupportEngagementService _engagements;

    public SupportEngagementsController(ISupportEngagementService engagements) => _engagements = engagements;

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartEngagementRequest request)
    {
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