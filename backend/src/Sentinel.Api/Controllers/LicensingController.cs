using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Licensing;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("licensing")]
[Authorize(Policy = "OwnerOnly")]
public class LicensingController : ControllerBase
{
    private readonly ILicenseService _licenses;

    public LicensingController(ILicenseService licenses) => _licenses = licenses;

    [HttpGet("{orgId:guid}")]
    public async Task<IActionResult> Get(Guid orgId)
    {
        var license = await _licenses.GetLicenseAsync(orgId);
        return license is null ? NotFound() : Ok(license);
    }

    [HttpPut("{orgId:guid}")]
    public async Task<IActionResult> UpdateCap(Guid orgId, [FromBody] int endpointCap)
    {
        await _licenses.UpdateEndpointCapAsync(orgId, endpointCap);
        return NoContent();
    }
}
