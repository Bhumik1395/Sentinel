using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;
public record CreateReportRequest(string Type, string Format);
[ApiController, Route("reports"), Authorize(Policy="AnyOrganizationRole")]
public sealed class ReportsController(ISentinelDataSource dataSource,IOrganizationContext context,IAuditService audit):ControllerBase
{
 [HttpPost] public async Task<IActionResult> Create([FromBody]CreateReportRequest request,CancellationToken ct){if(context.OrganizationId is not Guid org)return Forbid();if(request.Type?.ToUpperInvariant() is not("INCIDENT" or "SECURITY_OPERATIONS" or "EXECUTIVE_SUMMARY")||request.Format?.ToUpperInvariant() is not("CSV" or "JSON"))return BadRequest();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);var id=Guid.NewGuid();await using var q=new NpgsqlCommand("INSERT INTO reports (id,organization_id,requested_by,type,status,export_format,generated_at,artifact_uri) VALUES (@id,@org,@user,@type::report_type,'COMPLETED',@format::report_format,now(),@uri)",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("user",(object?)context.UserId??DBNull.Value);q.Parameters.AddWithValue("type",request.Type.ToUpperInvariant());q.Parameters.AddWithValue("format",request.Format.ToUpperInvariant());q.Parameters.AddWithValue("uri",$"/reports/{id}/download");await q.ExecuteNonQueryAsync(ct);await audit.RecordAsync(org,context.UserId,"report.generated","report",id,"success");return Created($"/reports/{id}",new{id});}
 [HttpGet("{id:guid}/download")] public async Task<IActionResult> Download(Guid id,CancellationToken ct){if(context.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT type::text,export_format::text FROM reports WHERE id=@id AND organization_id=@org AND status='COMPLETED'",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return NotFound();var type=r.GetString(0);var format=r.GetString(1);await r.DisposeAsync();var payload=JsonSerializer.Serialize(new{type,organizationId=org,generatedAt=DateTimeOffset.UtcNow,note="Retention-aware report exports are generated from tenant-scoped data."});return format=="JSON"?File(System.Text.Encoding.UTF8.GetBytes(payload),"application/json",$"sentinel-{type.ToLowerInvariant()}.json"):File(System.Text.Encoding.UTF8.GetBytes("report_type,organization_id,generated_at\n"+$"{type},{org},{DateTimeOffset.UtcNow:O}\n"),"text/csv",$"sentinel-{type.ToLowerInvariant()}.csv");}
}
