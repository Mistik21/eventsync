using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<int> SavedAsync();
    Task<User?> GetByTelegramIdAsync(long telegramId);
    void Add(User user);
}