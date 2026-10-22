using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController, Route("hunting"), Authorize(Policy="AnyOrganizationRole")]
public sealed class HuntingController(ISentinelDataSource dataSource, IOrganizationContext context) : ControllerBase
{
 [HttpGet("events")]
 public async Task<IActionResult> Search([FromQuery]string? query,[FromQuery]string? eventType,[FromQuery]int limit=100,CancellationToken ct=default)
 {
  if(context.OrganizationId is not Guid org)return Forbid();limit=Math.Clamp(limit,1,500);await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);
  await using var cmd=new NpgsqlCommand(@"SELECT id,event_id,hostname,event_type::text,severity::text,occurred_at,process_name,command_line,domain,ip_address::text FROM events WHERE organization_id=@org AND (@type IS NULL OR event_type::text=@type) AND (@query IS NULL OR hostname ILIKE '%'||@query||'%' OR process_name ILIKE '%'||@query||'%' OR command_line ILIKE '%'||@query||'%' OR domain ILIKE '%'||@query||'%') ORDER BY occurred_at DESC LIMIT @limit",c);
  cmd.Parameters.AddWithValue("org",org);cmd.Parameters.AddWithValue("type",(object?)eventType?.ToUpperInvariant()??DBNull.Value);cmd.Parameters.AddWithValue("query",(object?)query?.Trim()??DBNull.Value);cmd.Parameters.AddWithValue("limit",limit);var items=new List<object>();await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))items.Add(new{id=r.GetGuid(0),eventId=r.GetString(1),hostname=r.GetString(2),eventType=r.GetString(3),severity=r.GetString(4),occurredAt=r.GetFieldValue<DateTimeOffset>(5),processName=r.IsDBNull(6)?null:r.GetString(6),commandLine=r.IsDBNull(7)?null:r.GetString(7),domain=r.IsDBNull(8)?null:r.GetString(8),ipAddress=r.IsDBNull(9)?null:r.GetString(9)});return Ok(new{items});
 }
}
