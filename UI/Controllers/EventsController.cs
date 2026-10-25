using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMyEvents() => Ok();

    [HttpGet("{id:guid}")]
    public IActionResult GetEvent(Guid id) => Ok();

    [HttpPost]
    public IActionResult CreateEvent() => Ok();

    [HttpPatch("{id:guid}")]
    public IActionResult UpdateEvent(Guid id) => Ok();

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteEvent(Guid id) => NoContent();

    [HttpPost("{id:guid}/join")]
    public IActionResult JoinEvent(Guid id) => Ok();

    [HttpPost("{id:guid}/leave")]
    public IActionResult LeaveEvent(Guid id) => NoContent();

    [HttpGet("{id:guid}/participants")]
    public IActionResult GetParticipants(Guid id) => Ok();

    [HttpDelete("{eventId:guid}/participants/{userId:guid}")]
    public IActionResult RemoveParticipant(Guid eventId, Guid userId)
        => NoContent();

    [HttpPost("{eventId:guid}/pay-debt")]
    public IActionResult PayDebt(Guid eventId) => NoContent();

    [HttpPost("{eventId:guid}/collection/open")]
    public IActionResult OpenCollection(Guid eventId) => NoContent();
}