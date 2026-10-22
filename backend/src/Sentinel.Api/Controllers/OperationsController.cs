using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController, Route("operations"), Authorize(Policy="OwnerOnly")]
public sealed class OperationsController(ISentinelDataSource dataSource, IConfiguration configuration) : ControllerBase
{
 [HttpGet("readiness")]
 public async Task<IActionResult> Readiness(CancellationToken ct)
 {
  var checks=new List<object>(); var ready=true;
  try {await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT 1",c);await q.ExecuteScalarAsync(ct);checks.Add(new{name="database",status="HEALTHY"});}catch{ready=false;checks.Add(new{name="database",status="FAILED"});}
  var keycloak=!string.IsNullOrWhiteSpace(configuration["Keycloak:Authority"]); ready&=keycloak; checks.Add(new{name="keycloak",status=keycloak?"CONFIGURED":"FAILED"});
  var signing=!string.IsNullOrWhiteSpace(configuration["CommandSigning:ActiveSecret"]); ready&=signing; checks.Add(new{name="commandSigning",status=signing?"CONFIGURED":"FAILED"});
  return StatusCode(ready?200:503,new{ready,checks});
 }
}
