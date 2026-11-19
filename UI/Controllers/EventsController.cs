using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.UI.DTOs.Requests.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService eventService;

    public EventsController(IEventService eventService)
    {
        this.eventService = eventService;
    }

    [HttpGet]
    public IActionResult GetMyEvents() => Ok();

    [HttpGet("{id:guid}")]
    public IActionResult GetEvent(Guid id) => Ok();

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await eventService.CreateEventAsync(new EventCreationModel(
                request.Title,
                request.Description,
                request.EventStartTime,
                new Location(request.EventLocation.Address, request.EventLocation.Coordinates),
                request.Budget,
                request.MainImageUrl
                ), currentUserId);
        }
        catch (Exception)
        {
            return BadRequest();
        }
        return Ok();
    }

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
