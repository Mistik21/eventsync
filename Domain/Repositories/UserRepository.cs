using eventsynс.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eventsynс.Domain.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext db;

    public UserRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        return await db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException();
    }

    public async Task<User> GetByTelegramIdAsync(long telegramId)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.TelegramID == telegramId)
            ?? throw new KeyNotFoundException();
    }

    public async Task<int> SavedAsync()
    {
        return await db.SaveChangesAsync();
    }

    public void Add(User user)
    {
        db.Users.Add(user);
    }
}
