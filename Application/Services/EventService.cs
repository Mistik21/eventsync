using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

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
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        if (eventDetails == null || !eventDetails.IsParticipant(currentUserId))
        {
            throw new Exception();
        }

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
        eventRepository.Add(eventNew);
        await eventRepository.SavedAsync();
        user.IncrementAttendedEventCount();
        await userRepository.SavedAsync();
    }

    public Task UpdateEventAsync(Guid eventId, EventUpdateModel data, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        if (eventDetails is null)
        {
            return false;
        }

        if (!eventDetails.IsOrganizer(currentUserId))
        {
            throw new Exception();
        }

        foreach (var participant in eventDetails.Participants)
        {
            participant.DecrementAttendedEventCount();
        }

        eventRepository.Remove(eventDetails);
        await eventRepository.SavedAsync();
        return true;
    }

    public async Task<(Event Event, string? OrganizerName)?> GetInvitationAsync(Guid eventId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        if (eventDetails is null)
        {
            return null;
        }

        var organizer = await userRepository.GetByIdAsync(eventDetails.Organizer);
        return (eventDetails, organizer?.Name);
    }

    public async Task JoinEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        var user = await userRepository.GetByIdAsync(currentUserId);
        if (user == null || eventDetails == null || eventDetails.CollectionStatus == CollectionStatus.Open ||
            eventDetails.IsParticipant(currentUserId))
        {
            throw new Exception();
        }

        eventDetails.AddParticipant(user);
        user.IncrementAttendedEventCount();
        await eventRepository.SavedAsync();
        await userRepository.SavedAsync();
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

    public Task PayMyDebtAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task OpenCollectionAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }
}
