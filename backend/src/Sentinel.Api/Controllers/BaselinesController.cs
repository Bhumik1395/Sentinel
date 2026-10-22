using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

[ApiController, Route("baselines"), Authorize(Policy="SecurityAdministratorOrAbove")]
public sealed class BaselinesController(ISentinelDataSource dataSource, IOrganizationContext context, IAuditService audit) : ControllerBase
{
 [HttpPost("endpoints/{endpointId:guid}/measure")]
 public async Task<IActionResult> Measure(Guid endpointId,CancellationToken ct)
 { if(context.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT count(*),count(DISTINCT COALESCE(process_name,'')),count(DISTINCT COALESCE(domain,'')) FROM events WHERE organization_id=@org AND endpoint_id=@endpoint",c);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("endpoint",endpointId);await using var r=await q.ExecuteReaderAsync(ct);await r.ReadAsync(ct);var events=r.GetInt64(0);var features=JsonSerializer.Serialize(new{eventCount=events,distinctProcesses=r.GetInt64(1),distinctDomains=r.GetInt64(2),learningState=events<1000?"LEARNING":"READY"});await r.DisposeAsync();await using var insert=new NpgsqlCommand("INSERT INTO ml_baselines (organization_id,endpoint_id,model_version,learning_completed_at,feature_state) VALUES (@org,@endpoint,'v1',CASE WHEN @events>=1000 THEN now() ELSE NULL END,@features::jsonb) ON CONFLICT (endpoint_id,model_version) DO UPDATE SET feature_state=excluded.feature_state,learning_completed_at=excluded.learning_completed_at,updated_at=now() RETURNING id",c);insert.Parameters.AddWithValue("org",org);insert.Parameters.AddWithValue("endpoint",endpointId);insert.Parameters.AddWithValue("events",events);insert.Parameters.AddWithValue("features",features);var id=(Guid)(await insert.ExecuteScalarAsync(ct))!;await audit.RecordAsync(org,context.UserId,"baseline.measured","ml_baseline",id,events>=1000?"ready":"learning");return Ok(new{id,eventCount=events,featureState=JsonDocument.Parse(features).RootElement}); }
}
