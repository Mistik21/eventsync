using eventsynс.Application.Models;
using eventsynс.Application.Models.Event;
using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Interfaces.Services;

public interface IEventService
{
    Task<List<Event>> GetMyEventsAsync(Guid currentUserId);
    Task<Event> GetEventAsync(Guid eventId, Guid currentUserId);

    Task CreateEventAsync(EventCreationModel data, Guid currentUserId);
    Task UpdateEventAsync(Guid eventId, EventUpdateModel data, Guid currentUserId);
    Task DeleteEventAsync(Guid eventId, Guid currentUserId);

    Task JoinEventAsync(Guid eventId, Guid currentUserId);
    Task LeaveEventAsync(Guid eventId, Guid currentUserId);

    Task<List<User>> GetParticipantsAsync(Guid eventId, Guid currentUserId);
    Task RemoveParticipantAsync(Guid eventId, Guid userId, Guid currentUserId);

    Task PayMyDebtAsync(Guid eventId, Guid currentUserId);
    
    Task OpenCollectionAsync(Guid eventId, Guid currentUserId);
}