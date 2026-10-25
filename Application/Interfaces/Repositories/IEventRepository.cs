using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid eventId);

    Task<Event?> GetByItemIdAsync(Guid itemId);
    Task<Event?> GetByActivityIdAsync(Guid activityId);
    Task<Event?> GetByCarIdAsync(Guid carId);
    Task<Event?> GetByCostIdAsync(Guid costId);

    Task<List<Event>> GetByUserIdAsync(Guid userId);

    Task AddAsync(Event eventEntity);
    Task DeleteAsync(Event eventEntity);

    Task SaveChangesAsync();
}