// backend/src/Sentinel.Api/Controllers/ApplicationsController.cs — new file
// Exposes the onboarding flow that had no HTTP surface at all. Minimal
// routing to match the PRD API table (§10): POST /applications is public
// and unauthenticated by design (no org exists yet to authenticate against);
// everything else requires Owner/Support Team.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Identity.Onboarding;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationsService _applications;

    public ApplicationsController(IApplicationsService applications) => _applications = applications;

    [HttpPost]
    [AllowAnonymous] // PRD §10: public, unauthenticated, heavily rate-limited (rate limiting not yet implemented - separate gap)
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest request)
    {
        var id = await _applications.SubmitApplicationAsync(request);
        return CreatedAtAction(nameof(Submit), new { id }, new { id });
    }

    [HttpGet]
    [Authorize(Policy = "SentinelCompany")]
    public async Task<IActionResult> List() => Ok(await _applications.ListApplicationsAsync());

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "OwnerOnly")] // PRD: Owner-only final approval
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _applications.ApproveApplicationAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "SentinelCompany")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest body)
    {
        await _applications.RejectApplicationAsync(id, body.Reason);
        return NoContent();
    }
}

public record RejectRequest(string Reason);