using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/activities")]
public class ActivitiesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetActivities(Guid eventId) => Ok();

    [HttpGet("{activityId:guid}")]
    public IActionResult GetActivity(Guid eventId, Guid activityId) => Ok();

    [HttpPost]
    public IActionResult CreateActivity(Guid eventId) => Ok();

    [HttpPatch("{activityId:guid}")]
    public IActionResult UpdateActivity(Guid eventId, Guid activityId)
        => Ok();

    [HttpDelete("{activityId:guid}")]
    public IActionResult DeleteActivity(Guid eventId, Guid activityId)
        => NoContent();

    [HttpPost("{activityId:guid}/join")]
    public IActionResult JoinActivity(Guid eventId, Guid activityId)
        => Ok();

    [HttpPost("{activityId:guid}/leave")]
    public IActionResult LeaveActivity(Guid eventId, Guid activityId)
        => NoContent();
}