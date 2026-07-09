using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Identity.Organizations;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("organizations")]
[Authorize(Policy = "OwnerOnly")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationsService _organizations;

    public OrganizationsController(IOrganizationsService organizations) => _organizations = organizations;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
    {
        var result = await _organizations.CreateOrganizationAsync(request);

        return CreatedAtAction(nameof(Get), new { id = result.OrganizationId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await _organizations.ListOrganizationsAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var org = await _organizations.GetOrganizationAsync(id);

        return org is null ? NotFound() : Ok(org);
    }
}
