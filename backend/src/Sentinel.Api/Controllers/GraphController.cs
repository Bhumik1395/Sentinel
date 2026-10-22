using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController, Route("graph"), Authorize(Policy="AnyOrganizationRole")]
public sealed class GraphController(ISentinelDataSource dataSource,IOrganizationContext context):ControllerBase
{
 [HttpPost("rebuild")] [Authorize(Policy="SecurityAdministratorOrAbove")]
 public async Task<IActionResult> Rebuild(CancellationToken ct){if(context.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);var id=Guid.NewGuid();await using var q=new NpgsqlCommand("INSERT INTO graph_projection_runs (id,organization_id,status) VALUES (@id,@org,'PENDING')",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);await q.ExecuteNonQueryAsync(ct);return Accepted(new{id,status="PENDING"});}
 [HttpGet("status")]
 public async Task<IActionResult> Status(CancellationToken ct){if(context.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT id,status,started_at,completed_at,error_message FROM graph_projection_runs WHERE organization_id=@org ORDER BY created_at DESC LIMIT 1",c);q.Parameters.AddWithValue("org",org);await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return Ok(new{status="NOT_BUILT"});return Ok(new{id=r.GetGuid(0),status=r.GetString(1),startedAt=r.IsDBNull(2)?null:r.GetFieldValue<DateTimeOffset>(2),completedAt=r.IsDBNull(3)?null:r.GetFieldValue<DateTimeOffset>(3),error=r.IsDBNull(4)?null:r.GetString(4)});}
 [HttpGet("investigation")]
 public async Task<IActionResult> Investigation([FromQuery]Guid? alertId,CancellationToken ct){if(context.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT a.id,a.title,e.id,e.hostname FROM alerts a JOIN endpoints e ON e.id=a.endpoint_id WHERE a.organization_id=@org AND (@alert IS NULL OR a.id=@alert) ORDER BY a.last_seen_at DESC LIMIT 100",c);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("alert",(object?)alertId??DBNull.Value);var nodes=new List<object>();var edges=new List<object>();await using var r=await q.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct)){var alert=r.GetGuid(0);var endpoint=r.GetGuid(2);nodes.Add(new{id=$"alert:{alert}",kind="alert",label=r.GetString(1)});nodes.Add(new{id=$"endpoint:{endpoint}",kind="endpoint",label=r.GetString(3)});edges.Add(new{from=$"alert:{alert}",to=$"endpoint:{endpoint}",kind="observed_on"});}return Ok(new{nodes,edges});}
}
