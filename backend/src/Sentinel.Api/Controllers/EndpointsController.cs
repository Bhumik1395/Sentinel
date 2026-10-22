using Microsoft.AspNetCore.Mvc;
using Sentinel.Licensing;
using Sentinel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController]
[Route("endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly ILicenseService _licenses;
    private readonly IEnrollmentTokenService _tokens;
    private readonly IOrganizationContext _organization;
    private readonly IEndpointCertificateService _certificates;
    private readonly ISentinelDataSource _dataSource;

    public EndpointsController(ILicenseService licenses, IEnrollmentTokenService tokens, IOrganizationContext organization, IEndpointCertificateService certificates, ISentinelDataSource dataSource) => (_licenses, _tokens, _organization, _certificates, _dataSource) = (licenses, tokens, organization, certificates, dataSource);

    [HttpGet]
    [Authorize(Policy = "AnyOrganizationRole")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (_organization.OrganizationId is not Guid org) return Forbid();
        await using var connection = _dataSource.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"SELECT id,hostname,agent_version,policy_version,status::text,last_heartbeat_at,registered_at
FROM endpoints WHERE organization_id=@org AND status <> 'RETIRED' ORDER BY hostname", connection);
        command.Parameters.AddWithValue("org", org);
        var items = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new { id = reader.GetGuid(0), hostname = reader.GetString(1), agentVersion = reader.GetString(2), policyVersion = reader.GetInt32(3), status = reader.GetString(4), lastHeartbeatAt = reader.IsDBNull(5) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(5), registeredAt = reader.GetFieldValue<DateTimeOffset>(6) });
        return Ok(new { items });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("enrollment")]
    public async Task<IActionResult> Register([FromBody] RedeemEnrollmentTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname) || request.Hostname.Length > 255) return BadRequest();
        var organizationId = await _tokens.RedeemAsync(request);
        if (organizationId is null) return Unauthorized();
        var result = await _licenses.RegisterEndpointAsync(organizationId.Value, new RegisterEndpointRequest(request.Hostname, request.AgentVersion));
        return result.Outcome switch
        {
            RegistrationOutcome.Registered => Ok(await EnrollAsync(organizationId.Value, result.EndpointId!.Value)),
            RegistrationOutcome.CapReached => Conflict(new { error = "Endpoint cap reached for this organization." }),
            RegistrationOutcome.LicenseSuspended => Conflict(new { error = "Organization license is suspended." }),
            RegistrationOutcome.LicenseExpired => Conflict(new { error = "Organization license is expired." }),
            _ => StatusCode(500)
        };
    }

    private async Task<object> EnrollAsync(Guid organizationId, Guid endpointId)
    {
        var certificate = await _certificates.IssueAsync(organizationId, endpointId);
        return new { endpointId, certificate.PfxBase64, certificate.Password, certificate.Fingerprint, certificate.ExpiresAt };
    }

    [HttpPost("enrollment-tokens")]
    [Authorize(Policy = "SecurityAdministratorOrAbove")]
    public async Task<IActionResult> CreateToken([FromBody] CreateEnrollmentTokenRequest request)
    {
        if (_organization.OrganizationId is Guid org && org != request.OrganizationId && _organization.Role != "owner") return Forbid();
        return Ok(new { token = await _tokens.CreateAsync(request) });
    }

    [HttpPost("{id:guid}/retire")]
    [Authorize(Policy = "SecurityAdministratorOrAbove")]
    public async Task<IActionResult> Retire(Guid id)
    {
        if (_organization.OrganizationId is not Guid org) return Forbid();
        return await _licenses.RetireEndpointAsync(org, id) ? NoContent() : NotFound();
    }
}
