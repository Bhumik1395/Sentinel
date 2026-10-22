using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public record CreateSoarRequest(Guid EndpointId, string ActionType, string Justification, Guid? AlertId, Guid? IncidentId, JsonElement? Payload);
public record SoarDecisionRequest(bool Approve);

[ApiController, Route("soar"), Authorize(Policy = "AnyOrganizationRole")]
public sealed class SoarController(ISentinelDataSource dataSource, IOrganizationContext organization, IAuditService audit, ICommandSigningService signer) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) { if(organization.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("SELECT id,target_endpoint_id,action_type::text,status::text,justification,requested_at,approved_by FROM soar_actions WHERE organization_id=@org ORDER BY requested_at DESC",c);q.Parameters.AddWithValue("org",org);var items=new List<object>();await using var r=await q.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))items.Add(new{id=r.GetGuid(0),endpointId=r.GetGuid(1),actionType=r.GetString(2),status=r.GetString(3),justification=r.GetString(4),requestedAt=r.GetFieldValue<DateTimeOffset>(5),approvedBy=r.IsDBNull(6)?null:r.GetGuid(6)});return Ok(new{items}); }

    [HttpPost]
    public async Task<IActionResult> Request([FromBody] CreateSoarRequest request,CancellationToken ct)
    {
        if(organization.OrganizationId is not Guid org||organization.UserId is not Guid actor)return Forbid(); var type=request.ActionType?.ToUpperInvariant();
        if(type is not ("KILL_PROCESS" or "ENDPOINT_ISOLATION")||string.IsNullOrWhiteSpace(request.Justification)||request.Justification.Length>4000)return BadRequest(new{error="A supported action and justification are required."});
        await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);var id=Guid.NewGuid();await using var q=new NpgsqlCommand(@"INSERT INTO soar_actions (id,organization_id,requested_by,target_endpoint_id,alert_id,incident_id,action_type,justification,request_payload,status)
SELECT @id,@org,@actor,e.id,@alert,@incident,@type::soar_action_type,@why,@payload::jsonb,CASE WHEN o.approval_policy='SECURITY_ADMIN_DEFAULT' THEN 'PENDING_APPROVAL'::soar_action_status ELSE 'PENDING_APPROVAL'::soar_action_status END FROM endpoints e JOIN organizations o ON o.id=e.organization_id WHERE e.id=@endpoint AND e.organization_id=@org AND e.status <> 'RETIRED'",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("actor",actor);q.Parameters.AddWithValue("endpoint",request.EndpointId);q.Parameters.AddWithValue("alert",(object?)request.AlertId??DBNull.Value);q.Parameters.AddWithValue("incident",(object?)request.IncidentId??DBNull.Value);q.Parameters.AddWithValue("type",type);q.Parameters.AddWithValue("why",request.Justification.Trim());q.Parameters.AddWithValue("payload",request.Payload?.GetRawText()??"{}");if(await q.ExecuteNonQueryAsync(ct)==0)return NotFound();await audit.RecordAsync(org,actor,"soar.requested","soar_action",id,type);return Created($"/soar/{id}",new{id,status="PENDING_APPROVAL"});
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize(Policy="SecurityAdministratorOrAbove")]
    public async Task<IActionResult> Decide(Guid id,[FromBody] SoarDecisionRequest decision,CancellationToken ct)
    {
        if(organization.OrganizationId is not Guid org||organization.UserId is not Guid actor)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);
        Guid endpoint;string type;JsonElement payload;await using(var q=new NpgsqlCommand("UPDATE soar_actions SET status=CASE WHEN @approve THEN 'APPROVED'::soar_action_status ELSE 'DENIED'::soar_action_status END,approved_by=@actor,decided_at=now() WHERE id=@id AND organization_id=@org AND status='PENDING_APPROVAL' AND requested_by<>@actor RETURNING target_endpoint_id,action_type::text,request_payload",c,tx)){q.Parameters.AddWithValue("approve",decision.Approve);q.Parameters.AddWithValue("actor",actor);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return Conflict(new{error="Action is unavailable or approval separation failed."});endpoint=r.GetGuid(0);type=r.GetString(1);payload=JsonDocument.Parse(r.GetString(2)).RootElement.Clone();}
        if(decision.Approve){var commandId=Guid.NewGuid();var nonce=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));var expiry=DateTimeOffset.UtcNow.AddMinutes(5);var signature=signer.Sign(commandId,endpoint,type,nonce,expiry,payload);await using var q=new NpgsqlCommand("INSERT INTO agent_commands (id,organization_id,endpoint_id,soar_action_id,command_type,payload,status,nonce,expires_at,signature) VALUES (@command,@org,@endpoint,@soar,@type::soar_action_type,@payload::jsonb,'PENDING',@nonce,@expires,@signature)",c,tx);q.Parameters.AddWithValue("command",commandId);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("endpoint",endpoint);q.Parameters.AddWithValue("soar",id);q.Parameters.AddWithValue("type",type);q.Parameters.AddWithValue("payload",payload.GetRawText());q.Parameters.AddWithValue("nonce",nonce);q.Parameters.AddWithValue("expires",expiry);q.Parameters.AddWithValue("signature",signature);await q.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);await audit.RecordAsync(org,actor,decision.Approve?"soar.approved":"soar.denied","soar_action",id,"success");return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id,CancellationToken ct){if(organization.OrganizationId is not Guid org)return Forbid();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("UPDATE soar_actions SET status='CANCELLED'::soar_action_status WHERE id=@id AND organization_id=@org AND status IN ('PENDING_APPROVAL','APPROVED')",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);if(await q.ExecuteNonQueryAsync(ct)==0)return Conflict();await audit.RecordAsync(org,organization.UserId,"soar.cancelled","soar_action",id,"success");return NoContent();}
}
