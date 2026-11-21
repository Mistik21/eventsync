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

    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetEvent(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await eventService.GetEventAsync(eventId, currentUserId);
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
                result.IsParticipant(currentUserId),
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

    [HttpPatch("{eventId:guid}")]
    public async Task<IActionResult> UpdateEvent(UpdateEventRequest data, Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await eventService.UpdateEventAsync(
                eventId,
                new EventUpdateModel(
                    data.Title,
                    data.Description,
                    data.EventStartTime,
                    new Location(data.EventLocation.Address, data.EventLocation.Coordinates),
                    data.Budget,
                    data.MainImageUrl
                    ), currentUserId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var deleted = await eventService.DeleteEventAsync(eventId, currentUserId);
            return deleted ? NoContent() : NotFound();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpGet("{eventId:guid}/invitation")]
    public async Task<IActionResult> GetInvitation(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        var invitation = await eventService.GetInvitationAsync(eventId);
        if (invitation is null)
        {
            return NotFound();
        }

        var (eventDetails, organizerName) = invitation.Value;
        var isParticipant = eventDetails.IsParticipant(currentUserId);
        if (!isParticipant && eventDetails.IsOpenCollection())
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

    [HttpPost("{eventId:guid}/join")]
    public async Task<IActionResult> JoinEvent(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }
        try
        {
           await eventService.JoinEventAsync(eventId, currentUserId);
           return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost("{id:guid}/leave")]
    public IActionResult LeaveEvent(Guid id) => NoContent();

    [HttpGet("{eventId:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await eventService.GetParticipantsAsync(eventId, currentUserId);
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
    public async Task<IActionResult> PayDebt(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await eventService.PayMyDebtAsync(eventId, currentUserId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost("{eventId:guid}/collection/open")]
    public async Task<IActionResult> OpenCollection(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await eventService.OpenCollectionAsync(eventId, currentUserId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}
