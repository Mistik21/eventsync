using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public interface IEventRepository
{
    Task<List<Event>> ListForOrganizerOrParticipantAsync(Guid userId);
    Task<List<User>> ListParticipantsAsync(Guid eventId);
    Task<Event?> GetByIdAsync(Guid id);
    Task<int> CountByOrganizerAsync(Guid organizerId);
    Task<int> SavedAsync();
    void Add(Event user);
    void Remove(Event eventDetails);
    void RemoveItem(Item item);
}
