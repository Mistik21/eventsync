using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public interface IEventRepository
{
    Task<int> CountByOrganizerAsync(Guid organizerId);
}