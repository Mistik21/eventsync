using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByTelegramIdAsync(long telegramId);

    Task AddAsync(User user);
    Task SaveChangesAsync();
}