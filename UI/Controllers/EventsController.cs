using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.UI.DTOs.Requests.Events;
using eventsynс.UI.DTOs.Responses;
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
    public async Task<ActionResult<List<EventListItemResponse>>> GetMyEvents()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var events = await eventService.GetMyEventsAsync(currentUserId);
            return Ok(events.Select(e => new EventListItemResponse(
                e.Id,
                e.Title,
                e.Description,
                e.MainImageUrl,
                e.EventStartTime,
                new LocationResponse(e.EventLocation.Address, e.EventLocation.Coordinates),
                e.IsOrganizer(currentUserId))).ToList());
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await eventService.GetEventAsync(id, currentUserId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(new EventResponse(
                result.Id,
                result.Title,
                result.Description,
                result.MainImageUrl,
                result.EventStartTime,
                new LocationResponse(result.EventLocation.Address, result.EventLocation.Coordinates),
                result.Budget,
                result.Budget - result.Expenses.Sum(expense => expense.Amount),
                result.Organizer,
                result.Participants.Count,
                result.IsOrganizer(currentUserId),
                result.Participants.Any(participant => participant.Id == currentUserId),
                result.ImageEventUrl?.ToList() ?? [],
                result.CollectionStatus));
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

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
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var deleted = await eventService.DeleteEventAsync(id, currentUserId);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}/invitation")]
    public async Task<IActionResult> GetInvitation(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        var invitation = await eventService.GetInvitationAsync(id);
        if (invitation is null)
        {
            return NotFound();
        }

        var (eventDetails, organizerName) = invitation.Value;
        var isParticipant = eventDetails.Participants.Any(user => user.Id == currentUserId);
        if (!isParticipant && eventDetails.CollectionStatus != CollectionStatus.NotStarted)
        {
            return Conflict();
        }

        return Ok(new
        {
            eventDetails.Id,
            eventDetails.Title,
            eventDetails.MainImageUrl,
            eventDetails.EventStartTime,
            EventLocation = new LocationResponse(eventDetails.EventLocation.Address, eventDetails.EventLocation.Coordinates),
            OrganizerName = organizerName,
            ParticipantsCount = eventDetails.Participants.Count,
            IsParticipant = isParticipant
        });
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> JoinEvent(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }
        try
        {
           await eventService.JoinEventAsync(id, currentUserId);
           return Ok();
        }
        catch (Exception)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/leave")]
    public IActionResult LeaveEvent(Guid id) => NoContent();

    [HttpGet("{id:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await eventService.GetParticipantsAsync(id, currentUserId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result.Select(user => new { user.Id, user.Name }));
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpDelete("{eventId:guid}/participants/{userId:guid}")]
    public IActionResult RemoveParticipant(Guid eventId, Guid userId)
        => NoContent();

    [HttpPost("{eventId:guid}/pay-debt")]
    public IActionResult PayDebt(Guid eventId) => NoContent();

    [HttpPost("{eventId:guid}/collection/open")]
    public IActionResult OpenCollection(Guid eventId) => NoContent();
}
