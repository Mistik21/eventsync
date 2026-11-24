using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain;
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
        await eventRepository.SavedAsync();
    }

    public Task UpdateEventAsync(Guid eventId, EventUpdateModel data, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
            eventDetails.ChangeTitle(data.Title);
            eventDetails.ChangeDescription(data.Description);
            eventDetails.ChangeBudget(data.Budget);
            eventDetails.ChangeStartTime(data.EventStartTime);
            eventDetails.ChangeLocation(data.EventLocation);
            if (data.MainImageUrl != null) eventDetails.ChangeMainImage(data.MainImageUrl);
            return Task.CompletedTask;
        });
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        eventRepository.RemoveEvent(eventDetails);
        await eventRepository.SavedAsync();
        return true;
    }

    public async Task<(Event Event, string? OrganizerName)?> GetInvitationAsync(Guid eventId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var organizer = await userRepository.GetByIdAsync(eventDetails.Organizer);
        return (eventDetails, organizer.Name);
    }

    public Task JoinEventAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, async eventDetails =>
        {
            var user = await userRepository.GetByIdAsync(currentUserId);
            if (eventDetails.IsParticipant(currentUserId)) throw DomainConflict.CreateException();
            eventDetails.AddParticipant(user);
        });
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

    public Task PayMyDebtAsync(Guid eventId, Guid currentUserId, int amount)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
            eventDetails.PayDebt(currentUserId, amount);
            return Task.CompletedTask;
        });
    }

    public Task OpenCollectionAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
            eventDetails.OpenCollection();
            return Task.CompletedTask;
        });
    }

    public Task AddSupplementalDebtsAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId,  eventDetails =>
        {
            if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
            eventDetails.AddSupplementalDebts();
            return Task.CompletedTask;
        });
    }
}
