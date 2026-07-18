using Microsoft.AspNetCore.Mvc;
using Sentinel.Licensing;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly ILicenseService _licenses;

    public EndpointsController(ILicenseService licenses) => _licenses = licenses;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterEndpointRequest request)
    {
        var result = await _licenses.RegisterEndpointAsync(request);
        return result.Outcome switch
        {
            RegistrationOutcome.Registered => Ok(new { endpointId = result.EndpointId }),
            RegistrationOutcome.CapReached => Conflict(new { error = "Endpoint cap reached for this organization." }),
            RegistrationOutcome.LicenseSuspended => Conflict(new { error = "Organization license is suspended." }),
            RegistrationOutcome.LicenseExpired => Conflict(new { error = "Organization license is expired." }),
            _ => StatusCode(500)
        };
    }
}
