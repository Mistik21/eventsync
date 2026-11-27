using System.Data;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories.Interfaces;
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
    
    public async Task<int> CountByParticipantAsync(Guid userId)
    {
        return await db.Events.CountAsync(e => e.Participants.Any(p => p.Id == userId));
    }
    
    public async Task<Event> GetEventByIdAsync(Guid eventId)
    {
        
        var result = await db.Events
            .Include(e => e.Participants)
            .Include(e => e.Expenses)
            .Include(e => e.Items)
            .Include(e => e.Activities)
            .ThenInclude(activity => activity.Participants)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == eventId);
        return result ?? throw new KeyNotFoundException();
    }

    public async Task SaveEventChangesAsync(Guid eventId, Func<Event, Task> change)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       SELECT 1 FROM "Events"
                                                       WHERE "Id" = {eventId} 
                                                       FOR UPDATE
                                                       """);

        var eventDetails = await GetEventByIdAsync(eventId);
        await change(eventDetails);
        await SavedAsync();
        await transaction.CommitAsync();
    }

    public void AddEvent(Event eventDetails)
    {
        db.Events.Add(eventDetails);
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
}
