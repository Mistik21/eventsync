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

    public async Task<int> CountByOrganizerAsync(Guid organizerId)
    {
        return await db.Events.CountAsync(e => e.Organizer == organizerId);
    }

    public async Task<Event> GetEventByIdAsync(Guid eventId)
    {
        var result=await db.Events
            .Include(e => e.Participants)
            .Include(e => e.Expenses)
            .Include(e => e.Items)
            .Include(e => e.Activities)
                .ThenInclude(activity => activity.Participants)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == eventId);
        return result ?? throw new KeyNotFoundException();
    }

    public void AddEvent(Event @event)
    {
        db.Events.Add(@event);
    }

    public void RemoveEvent(Event eventDetails)
    {
        db.Events.Remove(eventDetails);
    }
    public void RemoveItem(Item item)
    {
        db.Items.Remove(item);
    }
    
    public void RemoveCost(Cost cost)
    {
        db.Costs.Remove(cost);
    }
    public void RemoveActivity(Activity activity)
    {
        db.Activities.Remove(activity);
    }
    
    public async Task<int> SavedAsync()
    {
        return await db.SaveChangesAsync();
    }

    public void ClearTracking() => db.ChangeTracker.Clear();
}
