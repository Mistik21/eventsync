using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace eventsynс.Application.Services;

public class EventService : IEventService
{
    private readonly IUserRepository userRepository;
    private readonly IEventRepository eventRepository;

    public EventService(IUserRepository userRepository, IEventRepository eventRepository)
    {
        this.userRepository = userRepository;
        this.eventRepository = eventRepository;
    }

    public async Task<List<Event>> GetMyEventsAsync(Guid currentUserId)
    {
        return await eventRepository.ListForOrganizerOrParticipantAsync(currentUserId);
    }

    public async Task<Event> GetEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
        return eventDetails;
    }

    public async Task CreateEventAsync(EventCreationModel data, Guid currentUserId)
    {
        var eventNew = new Event(
            Guid.NewGuid(),
            data.Title,
            data.Description,
            data.MainImageUrl,
            data.EventStartTime,
            data.EventLocation,
            data.Budget,
            currentUserId);
        var user = await userRepository.GetByIdAsync(currentUserId);
        eventNew.AddParticipant(user);
        eventRepository.AddEvent(eventNew);
        user.IncrementAttendedEventCount();
        await eventRepository.SavedAsync();
    }

    public async Task UpdateEventAsync(Guid eventId, EventUpdateModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        eventDetails.ChangeTitle(data.Title);
        eventDetails.ChangeDescription(data.Description);
        eventDetails.ChangeBudget(data.Budget);
        eventDetails.ChangeStartTime(data.EventStartTime);
        eventDetails.ChangeLocation(data.EventLocation);
        if (data.MainImageUrl != null) eventDetails.ChangeMainImage(data.MainImageUrl);
        await eventRepository.SavedAsync();
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        foreach (var participant in eventDetails.Participants)
        {
            participant.DecrementAttendedEventCount();
        }

        eventRepository.RemoveEvent(eventDetails);
        await eventRepository.SavedAsync();
        return true;
    }

    public async Task<(Event Event, string? OrganizerName)?> GetInvitationAsync(Guid eventId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var organizer = await userRepository.GetByIdAsync(eventDetails.Organizer);
        return (eventDetails, organizer?.Name);
    }

    public async Task JoinEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var user = await userRepository.GetByIdAsync(currentUserId);
        if (eventDetails.IsParticipant(currentUserId)) throw DomainConflict.CreateException();
        eventDetails.AddParticipant(user);
        user.IncrementAttendedEventCount();
        await eventRepository.SavedAsync();
    }

    public Task LeaveEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<User>?> GetParticipantsAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await GetEventAsync(eventId, currentUserId);
        return eventDetails.Participants.ToList();
    }

    public Task RemoveParticipantAsync(Guid eventId, Guid userId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task PayMyDebtAsync(Guid eventId, Guid currentUserId)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
            if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();

            eventDetails.PayDebt(currentUserId);

            try
            {
                await eventRepository.SavedAsync();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                eventRepository.ClearTracking();
            }
        }
    }

    public async Task OpenCollectionAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        eventDetails.OpenCollection();
        await eventRepository.SavedAsync();
    }
}
