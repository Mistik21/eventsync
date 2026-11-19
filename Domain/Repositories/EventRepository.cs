using eventsynс.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eventsynс.Domain.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext db;

    public EventRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<int> CountByOrganizerAsync(Guid organizerId)
    {
        return await db.Events.CountAsync(e => e.Organizer == organizerId);
    }
}