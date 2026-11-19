using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class EventService:IEventService
{
    private readonly IUserRepository userRepository;
    private readonly IEventRepository eventRepository;
    
    public EventService(IUserRepository userRepository,IEventRepository eventRepository)
    {
        this.userRepository = userRepository;
        this.eventRepository = eventRepository;
    }
    
    public Task<List<Event>> GetMyEventsAsync(Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task<Event> GetEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task CreateEventAsync(EventCreationModel data, Guid currentUserId)
    {
        var eventNew= new Event(
            Guid.NewGuid() ,
            data.Title,
            data.Description,
            data.MainImageUrl,
            data.EventStartTime,
            data.EventLocation,
            data.Budget,
            currentUserId);
        eventRepository.Add(eventNew);
        await eventRepository.SavedAsync();
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

    public Task<List<User>> GetParticipantsAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
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