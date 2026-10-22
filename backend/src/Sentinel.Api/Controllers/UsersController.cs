using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Identity;

namespace Sentinel.Api.Controllers;
public record InviteUserRequest(string Email, string DisplayName, string Role);
[ApiController, Route("users"), Authorize(Policy="SecurityAdministratorOrAbove")]
public sealed class UsersController(IUserManagementService users, IOrganizationContext context, IAuditService audit) : ControllerBase
{
 [HttpPost] public async Task<IActionResult> Invite([FromBody] InviteUserRequest request){if(context.OrganizationId is not Guid org||context.UserId is not Guid actor)return Forbid();var id=await users.InviteAsync(org,actor,context.Role,request.Role,request.Email,request.DisplayName);await audit.RecordAsync(org,actor,"user.invited","user",id,"success");return Created($"/users/{id}",new{id});}
 [HttpPost("{id:guid}/deactivate")] public async Task<IActionResult> Deactivate(Guid id){if(context.OrganizationId is not Guid org)return Forbid();await users.SetActiveAsync(org,id,false);await audit.RecordAsync(org,context.UserId,"user.deactivated","user",id,"success");return NoContent();}
 [HttpPost("{id:guid}/reactivate")] public async Task<IActionResult> Reactivate(Guid id){if(context.OrganizationId is not Guid org)return Forbid();await users.SetActiveAsync(org,id,true);await audit.RecordAsync(org,context.UserId,"user.reactivated","user",id,"success");return NoContent();}
}
