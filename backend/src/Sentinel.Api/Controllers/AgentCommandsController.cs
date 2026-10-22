using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public record AgentCommandResult(string Nonce, bool Succeeded, JsonElement? Result);

[ApiController, Route("agent/commands"), AllowAnonymous]
public sealed class AgentCommandsController(IAgentCertificateValidator certificates, ISentinelDataSource dataSource) : ControllerBase
{
    [HttpGet("next")]
    public async Task<IActionResult> Next(CancellationToken ct){var endpoint=await certificates.ValidateAsync(HttpContext);if(endpoint is null)return Unauthorized();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);await using var q=new NpgsqlCommand(@"WITH next AS (SELECT id FROM agent_commands WHERE endpoint_id=@endpoint AND status='PENDING' AND expires_at>now() ORDER BY created_at FOR UPDATE SKIP LOCKED LIMIT 1) UPDATE agent_commands a SET status='DISPATCHED',fetched_at=now() FROM next WHERE a.id=next.id RETURNING a.id,a.command_type::text,a.payload,a.nonce,a.expires_at,a.signature",c,tx);q.Parameters.AddWithValue("endpoint",endpoint.Value);await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct)){await tx.CommitAsync(ct);return NoContent();}var output=new{id=r.GetGuid(0),commandType=r.GetString(1),payload=JsonDocument.Parse(r.GetString(2)).RootElement.Clone(),nonce=r.GetString(3),expiresAt=r.GetFieldValue<DateTimeOffset>(4),signature=r.GetString(5)};await r.DisposeAsync();await tx.CommitAsync(ct);return Ok(output);}
    [HttpPost("{id:guid}/result")]
    public async Task<IActionResult> Result(Guid id,[FromBody] AgentCommandResult result,CancellationToken ct){var endpoint=await certificates.ValidateAsync(HttpContext);if(endpoint is null)return Unauthorized();if(string.IsNullOrWhiteSpace(result.Nonce))return BadRequest();await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);await using var q=new NpgsqlCommand(@"UPDATE agent_commands SET status=CASE WHEN @ok THEN 'EXECUTED' ELSE 'FAILED' END,acknowledged_at=now(),result=@result::jsonb,result_reported_at=now() WHERE id=@id AND endpoint_id=@endpoint AND nonce=@nonce AND status='DISPATCHED' AND expires_at>now() RETURNING soar_action_id",c,tx);q.Parameters.AddWithValue("ok",result.Succeeded);q.Parameters.AddWithValue("result",result.Result?.GetRawText()??"{}");q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("endpoint",endpoint.Value);q.Parameters.AddWithValue("nonce",result.Nonce);var soar=await q.ExecuteScalarAsync(ct);if(soar is not Guid action){await tx.RollbackAsync(ct);return Conflict(new{error="Command cannot be replayed or has expired."});}await using var a=new NpgsqlCommand("UPDATE soar_actions SET status=CASE WHEN @ok THEN 'EXECUTED'::soar_action_status ELSE 'FAILED'::soar_action_status END,result=@result::jsonb,executed_at=now() WHERE id=@id",c,tx);a.Parameters.AddWithValue("ok",result.Succeeded);a.Parameters.AddWithValue("result",result.Result?.GetRawText()??"{}");a.Parameters.AddWithValue("id",action);await a.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return NoContent();}
}
