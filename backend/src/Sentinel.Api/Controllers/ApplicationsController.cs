using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Identity.Onboarding;

namespace Sentinel.Api.Controllers;

public record RejectApplicationRequest(string Reason);

[ApiController]
[Route("applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationsService _applications;

    public ApplicationsController(IApplicationsService applications) => _applications = applications;

    // Public: prospective customers apply without an account.
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("applications-submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName) || request.CompanyName.Length > 200)
            return BadRequest(new { error = "companyName is required (max 200 characters)." });
        if (string.IsNullOrWhiteSpace(request.ContactName) || request.ContactName.Length > 200)
            return BadRequest(new { error = "contactName is required (max 200 characters)." });
        if (request.ContactEmail is not { Length: > 0 and <= 320 }
            || !MailAddress.TryCreate(request.ContactEmail, out _))
            return BadRequest(new { error = "contactEmail must be a valid email address." });
        if (request.RequestedEndpointCap is < 1 or > 1_000_000)
            return BadRequest(new { error = "requestedEndpointCap must be between 1 and 1,000,000." });

        var id = await _applications.SubmitApplicationAsync(request);
        return Accepted(new { applicationId = id });
    }

    [HttpGet]
    [Authorize(Policy = "SentinelCompany")]
    public async Task<IActionResult> List()
        => Ok(await _applications.ListApplicationsAsync());

    // PENDING -> IN_REVIEW
    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = "SentinelCompany")]
    public async Task<IActionResult> Review(Guid id)
    {
        // reviewed_by references users(id), and Owner / Support accounts are not rows in
        // `users` yet, so we cannot record the reviewer. Revisit once they are.
        await _applications.MarkInReviewAsync(id, null);
        return NoContent();
    }

    // IN_REVIEW -> IN_DISCUSSION
    [HttpPost("{id:guid}/discuss")]
    [Authorize(Policy = "SentinelCompany")]
    public async Task<IActionResult> Discuss(Guid id)
    {
        await _applications.MarkInDiscussionAsync(id);
        return NoContent();
    }

    // IN_DISCUSSION -> APPROVED. Creates the org, first CSO and license.
    // The response contains the CSO's one-time temporary password.
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Approve(Guid id)
        => Ok(await _applications.ApproveApplicationAsync(id));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectApplicationRequest request)
    {
        await _applications.RejectApplicationAsync(id, request.Reason);
        return NoContent();
    }
}
