using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.Auth;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.UI.DTOs.Requests.Costs;
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
    private readonly IEventImageStorage imageStorage;

    public EventsController(IEventService eventService, IEventImageStorage imageStorage)
    {
        this.eventService = eventService;
        this.imageStorage = imageStorage;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventListItemResponse>>> GetMyEvents()
    {
        var currentUserId = User.GetCurrentUserId();

        var events = await eventService.GetMyEventsAsync(currentUserId);
        return Ok(events.Select(e => new EventListItemResponse(
            e.Id,
            e.Title,
            e.Description,
            imageStorage.GetCoverUrl(e.Id, e.MainImageUrl),
            e.EventStartTime,
            new LocationResponse(e.EventLocation.Address, e.EventLocation.Coordinates),
            e.IsOrganizer(currentUserId))).ToList());
    }

    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetEvent(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();

        var result = await eventService.GetEventAsync(eventId, currentUserId);
        return Ok(new EventResponse(
            result.Id,
            result.Title,
            result.Description,
            imageStorage.GetCoverUrl(result.Id, result.MainImageUrl),
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

    [HttpPost]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        var currentUserId = User.GetCurrentUserId();

        await eventService.CreateEventAsync(new EventCreationModel(
            request.Title,
            request.Description,
            request.EventStartTime,
            new Location(request.EventLocation.Address, request.EventLocation.Coordinates),
            request.Budget,
            request.MainImageUrl), currentUserId);
        return Ok();
    }

    [HttpPatch("{eventId:guid}")]
    public async Task<IActionResult> UpdateEvent(UpdateEventRequest data, Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        await eventService.UpdateEventAsync(
            eventId,
            new EventUpdateModel(
                data.Title,
                data.Description,
                data.EventStartTime,
                new Location(data.EventLocation.Address, data.EventLocation.Coordinates),
                data.Budget,
                data.MainImageUrl),
            currentUserId);
        return Ok();
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        var deleted = await eventService.DeleteEventAsync(eventId, currentUserId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{eventId:guid}/invitation")]
    public async Task<IActionResult> GetInvitation(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        var (eventDetails, organizerName) = await eventService.GetInvitationAsync(eventId);
        var isParticipant = eventDetails.IsParticipant(currentUserId);
        if (!isParticipant && eventDetails.IsOpenCollection())
        {
            return Conflict();
        }

        return Ok(new
        {
            eventDetails.Id,
            eventDetails.Title,
            MainImageUrl = imageStorage.GetCoverUrl(eventDetails.Id, eventDetails.MainImageUrl),
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
        var currentUserId = User.GetCurrentUserId();
        await eventService.JoinEventAsync(eventId, currentUserId);
        return Ok();
    }

    [HttpPost("{id:guid}/leave")]
    public IActionResult LeaveEvent(Guid id) => NoContent();

    [HttpGet("{eventId:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        var participants = await eventService.GetParticipantsAsync(eventId, currentUserId);
        return Ok(participants.Select(user => new { user.Id, user.Name }));
    }

    [HttpDelete("{eventId:guid}/participants/{userId:guid}")]
    public IActionResult RemoveParticipant(Guid eventId, Guid userId)
        => NoContent();

    [HttpPost("{eventId:guid}/pay-debt")]
    public async Task<IActionResult> PayDebt(PayDebtRequests data, Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        await eventService.PayMyDebtAsync(eventId, currentUserId, data.Amount);
        return Ok();
    }

    [HttpPost("{eventId:guid}/collection/open")]
    public async Task<IActionResult> OpenCollection(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        await eventService.OpenCollectionAsync(eventId, currentUserId);
        return Ok();
    }

    [HttpPost("{eventId:guid}/collection/supplemental")]
    public async Task<IActionResult> AddSupplementalDebts(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        await eventService.AddSupplementalDebtsAsync(eventId, currentUserId);
        return Ok();
    }
}
