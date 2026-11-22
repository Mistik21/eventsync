using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User> GetByTelegramIdAsync(long telegramId);
    Task<int> SavedAsync();
    void Add(User user);
}