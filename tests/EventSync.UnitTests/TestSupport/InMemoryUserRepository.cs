using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace EventSync.UnitTests.TestSupport;

internal sealed class InMemoryUserRepository(params User[] users) : IUserRepository
{
    public List<User> Users { get; } = [.. users];
    public int SaveCount { get; private set; }

    public Task<User> GetByIdAsync(Guid id) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Id == id) ?? throw new KeyNotFoundException());

    public Task<User> GetByTelegramIdAsync(long telegramId) =>
        Task.FromResult(Users.SingleOrDefault(user => user.TelegramID == telegramId) ?? throw new KeyNotFoundException());

    public void Add(User user) => Users.Add(user);

    public Task<int> SavedAsync()
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}
