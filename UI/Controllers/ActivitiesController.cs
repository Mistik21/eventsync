using eventsynс.Application.Interfaces.Events;
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
        var currentUserId = User.GetCurrentUserId();

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
        var currentUserId = User.GetCurrentUserId();
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
        var currentUserId = User.GetCurrentUserId();

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
        var currentUserId = User.GetCurrentUserId();
        
        await activityService.DeleteActivityAsync(eventId, activityId, currentUserId);
        return Ok();
    }

    [HttpPost("{activityId:guid}/join")]
    public async Task<IActionResult> JoinActivity(Guid eventId, Guid activityId)
    {
        var currentUserId = User.GetCurrentUserId();
        
        await activityService.JoinActivityAsync(eventId, activityId, currentUserId);
        return Ok();
    }

    [HttpPost("{activityId:guid}/leave")]
    public async Task<IActionResult> LeaveActivity(Guid eventId, Guid activityId)
    {
        var currentUserId = User.GetCurrentUserId();
        
        await activityService.LeaveActivityAsync(eventId, activityId, currentUserId);
        return Ok();
    }
}
