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

    public async Task<Event?> GetEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventThis= await eventRepository.GetByIdAsync(eventId);
        if (eventThis != null && eventThis.Participants.All(x => x.Id != currentUserId))
        {
            throw new Exception();
        }
        return eventThis;
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

    public Task DeleteEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task JoinEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task LeaveEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<User>?> GetParticipantsAsync(Guid eventId, Guid currentUserId)
    {
        var participants= await eventRepository.ListParticipantsAsync(eventId);
        if (participants != null && participants.All(x => x.Id != currentUserId))
        {
            throw new Exception();
        }
        return participants;
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