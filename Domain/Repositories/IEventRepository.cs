using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public interface IEventRepository
{
    Task<List<Event>> ListForOrganizerOrParticipantAsync(Guid userId);
    Task<Event> GetEventByIdAsync(Guid id);
    Task<int> CountByOrganizerAsync(Guid organizerId);
    Task<int> CountByParticipantAsync(Guid userId);
    Task<int> SavedAsync();
    Task SaveEventChangesAsync(Guid eventId, Func<Event, Task> change);
    void AddEvent(Event user);
    void RemoveEvent(Event eventDetails);
    void RemoveCost(Cost cost);
    void RemoveActivity(Activity activity);
    void RemoveItem(Item item);
}
