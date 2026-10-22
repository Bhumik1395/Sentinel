using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sentinel.Identity;
using Sentinel.Identity.Data;

namespace Sentinel.Api.Controllers;

public record CreateIncidentRequest(string Title, string Severity, Guid? AlertId, Guid? AssignedTo, string? Summary);
public record IncidentNoteRequest(string Note);
public record IncidentStatusRequest(string Status, Guid? AssignedTo, string? Summary);

[ApiController, Route("incidents"), Authorize(Policy = "AnyOrganizationRole")]
public sealed class IncidentsController(ISentinelDataSource dataSource, IOrganizationContext organization, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (organization.OrganizationId is not Guid org) return Forbid();
        await using var c = dataSource.CreateConnection(); await c.OpenAsync(ct);
        await using var q = new NpgsqlCommand("SELECT id,title,severity::text,status::text,assigned_to,opened_at,summary FROM incidents WHERE organization_id=@org ORDER BY opened_at DESC", c);
        q.Parameters.AddWithValue("org", org); var items = new List<object>(); await using var r = await q.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) items.Add(new { id=r.GetGuid(0), title=r.GetString(1), severity=r.GetString(2), status=r.GetString(3), assignedTo=r.IsDBNull(4)?null:r.GetGuid(4), openedAt=r.GetFieldValue<DateTimeOffset>(5), summary=r.IsDBNull(6)?null:r.GetString(6) });
        return Ok(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request, CancellationToken ct)
    {
        if (organization.OrganizationId is not Guid org || organization.UserId is not Guid actor) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 240 || request.Severity?.ToUpperInvariant() is not ("LOW" or "MEDIUM" or "HIGH" or "CRITICAL")) return BadRequest(new { error="Valid title and severity are required." });
        await using var c=dataSource.CreateConnection(); await c.OpenAsync(ct); await using var tx=await c.BeginTransactionAsync(ct); var id=Guid.NewGuid();
        await using (var q=new NpgsqlCommand("INSERT INTO incidents (id,organization_id,title,severity,assigned_to,summary) VALUES (@id,@org,@title,@severity::severity_level,@assigned,@summary)",c,tx)) { q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("title",request.Title.Trim());q.Parameters.AddWithValue("severity",request.Severity.ToUpperInvariant());q.Parameters.AddWithValue("assigned",(object?)request.AssignedTo??DBNull.Value);q.Parameters.AddWithValue("summary",(object?)request.Summary??DBNull.Value);await q.ExecuteNonQueryAsync(ct); }
        if (request.AlertId is Guid alert) { await using var q=new NpgsqlCommand("INSERT INTO incident_alerts (incident_id,alert_id) SELECT @incident,id FROM alerts WHERE id=@alert AND organization_id=@org ON CONFLICT DO NOTHING",c,tx);q.Parameters.AddWithValue("incident",id);q.Parameters.AddWithValue("alert",alert);q.Parameters.AddWithValue("org",org);await q.ExecuteNonQueryAsync(ct); }
        await tx.CommitAsync(ct); await audit.RecordAsync(org,actor,"incident.created","incident",id,"success"); return Created($"/incidents/{id}",new { id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IncidentStatusRequest request, CancellationToken ct)
    {
        if (organization.OrganizationId is not Guid org) return Forbid(); var status=request.Status?.ToUpperInvariant();
        if (status is not ("OPEN" or "INVESTIGATING" or "CONTAINED" or "RESOLVED" or "CLOSED")) return BadRequest(new { error="Unsupported incident status." });
        await using var c=dataSource.CreateConnection(); await c.OpenAsync(ct); await using var q=new NpgsqlCommand("UPDATE incidents SET status=@status::incident_status,assigned_to=COALESCE(@assigned,assigned_to),summary=COALESCE(@summary,summary),resolved_at=CASE WHEN @status='RESOLVED' THEN now() ELSE resolved_at END,closed_at=CASE WHEN @status='CLOSED' THEN now() ELSE closed_at END WHERE id=@id AND organization_id=@org",c);q.Parameters.AddWithValue("status",status);q.Parameters.AddWithValue("assigned",(object?)request.AssignedTo??DBNull.Value);q.Parameters.AddWithValue("summary",(object?)request.Summary??DBNull.Value);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org); if(await q.ExecuteNonQueryAsync(ct)==0)return NotFound(); await audit.RecordAsync(org,organization.UserId,"incident.updated","incident",id,status);return NoContent();
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> Note(Guid id,[FromBody] IncidentNoteRequest request,CancellationToken ct)
    { if(organization.OrganizationId is not Guid org||organization.UserId is not Guid actor)return Forbid(); if(string.IsNullOrWhiteSpace(request.Note)||request.Note.Length>10000)return BadRequest(); await using var c=dataSource.CreateConnection();await c.OpenAsync(ct);await using var q=new NpgsqlCommand("INSERT INTO incident_notes (incident_id,author_id,note) SELECT id,@actor,@note FROM incidents WHERE id=@id AND organization_id=@org",c);q.Parameters.AddWithValue("id",id);q.Parameters.AddWithValue("org",org);q.Parameters.AddWithValue("actor",actor);q.Parameters.AddWithValue("note",request.Note.Trim());if(await q.ExecuteNonQueryAsync(ct)==0)return NotFound();await audit.RecordAsync(org,actor,"incident.note_added","incident",id,"success");return NoContent(); }
}
