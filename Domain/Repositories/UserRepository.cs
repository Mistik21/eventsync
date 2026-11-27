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

    public async Task<Dictionary<long, List<(string EventTitle, int Amount)>>> ListDebtorsAsync()
    {
        var events = await db.Events
            .Select(e => new { e.Title, e.Debts })
            .ToListAsync();
        var debtsByUser = events
            .SelectMany(e => e.Debts,
                (e, debt) => (UserId: debt.Key, EventTitle: e.Title, Amount: debt.Value))
            .Where(debt => debt.Amount > 0)
            .GroupBy(debt => debt.UserId, debt => (debt.EventTitle, debt.Amount))
            .ToDictionary(group => group.Key, group => group.ToList());

        if (debtsByUser.Count == 0)
            return [];

        var debtorIds = debtsByUser.Keys.ToArray();
        return await db.Users
            .Where(user => debtorIds.Contains(user.Id))
            .Select(user => new { user.Id, user.TelegramID })
            .ToDictionaryAsync(user => user.TelegramID, user => debtsByUser[user.Id]);
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
