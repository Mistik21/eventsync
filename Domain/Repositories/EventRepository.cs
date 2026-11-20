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

    public async Task<List<Event>> ListForOrganizerOrParticipantAsync(Guid userId)
    {
       return await db.Events
            .AsNoTracking()
            .Where(e => e.Organizer == userId || e.Participants.Any(p => p.Id == userId))
            .OrderBy(e => e.EventStartTime)
            .ThenBy(e => e.Id)
            .ToListAsync();
    }

    public async Task<List<User>> ListParticipantsAsync(Guid eventId)
    {
        return await db.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .SelectMany(e => e.Participants)
            .ToListAsync();
    }

    public async Task<int> CountByOrganizerAsync(Guid organizerId)
    {
        return await db.Events.CountAsync(e => e.Organizer == organizerId);
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await db.Events
            .Include(e => e.Participants)
            .Include(e => e.Expenses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == id);
    }

    public void Add(Event @event)
    {
        db.Events.Add(@event);
    }
    
    public async Task<int> SavedAsync()
    {
        return await db.SaveChangesAsync();
    }
}
