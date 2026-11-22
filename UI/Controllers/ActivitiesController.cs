using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Activity;
using eventsynс.UI.Auth;
using eventsynс.UI.DTOs.Requests.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/events/{eventId:guid}/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService activityService;

    public ActivitiesController(IActivityService activityService)
    {
        this.activityService = activityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActivities(Guid eventId)
    {
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await activityService.GetActivitiesAsync(eventId, currentUserId);
        return Ok(result.Select(activity => new
        {
            activity.Id,
            activity.Title,
            activity.Description,
            activity.Creator,
            activity.АctivityStartTime,
            activity.АctivityEndTime,
            activity.MaxParticipants,
            Participants = activity.Participants.Select(user => new { user.Id, user.Name })
        }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateActivity(CreateActivityRequest data, Guid eventId)
    {
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }
        await activityService.CreateActivityAsync(
            eventId,
            new ActivityCreationModel(
                data.Title,
                data.Description,
                data.ActivityStartTime,
                data.ActivityEndTime,
                data.MaxParticipants
            ), currentUserId);
        return Ok();
    }

    [HttpPatch("{activityId:guid}")]
    public async Task<IActionResult> UpdateActivity(UpdateActivityRequest data,Guid eventId, Guid activityId)
    {
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        await activityService.UpdateActivityAsync(
            eventId,
            activityId,
            new ActivityUpdateModel(
                data.Title, 
                data.Description, 
                data.ActivityStartTime, 
                data.ActivityEndTime,
                data.MaxParticipants
                ),
            currentUserId);
        return Ok();
    }

    [HttpDelete("{activityId:guid}")]
    public async Task<IActionResult> DeleteActivity(Guid eventId, Guid activityId)
    {
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }
        
        await activityService.DeleteActivityAsync(eventId, activityId, currentUserId);
        return Ok();
    }

    [HttpPost("{activityId:guid}/join")]
    public IActionResult JoinActivity(Guid eventId, Guid activityId)
        => Ok();

    [HttpPost("{activityId:guid}/leave")]
    public IActionResult LeaveActivity(Guid eventId, Guid activityId)
        => NoContent();
}
