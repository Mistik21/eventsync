using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace EventSync.UnitTests.TestSupport;

internal sealed class InMemoryEventRepository(params Event[] events) : IEventRepository
{
    public List<Event> Events { get; } = [.. events];
    public List<Activity> RemovedActivities { get; } = [];
    public List<Item> RemovedItems { get; } = [];
    public int SaveCount { get; private set; }

    public Task<Event> GetEventByIdAsync(Guid id) =>
        Task.FromResult(Events.SingleOrDefault(meetup => meetup.Id == id) ?? throw new KeyNotFoundException());

    public Task<List<Event>> ListForOrganizerOrParticipantAsync(Guid userId) =>
        Task.FromResult(Events.Where(meetup => meetup.IsOrganizer(userId) || meetup.IsParticipant(userId)).ToList());

    public Task<int> CountByOrganizerAsync(Guid organizerId) =>
        Task.FromResult(Events.Count(meetup => meetup.IsOrganizer(organizerId)));

    public Task<int> CountByParticipantAsync(Guid userId) =>
        Task.FromResult(Events.Count(meetup => meetup.IsParticipant(userId)));

    public void AddEvent(Event meetup) => Events.Add(meetup);

    public void RemoveEvent(Event meetup) => Events.Remove(meetup);

    public void RemoveActivity(Activity activity) => RemovedActivities.Add(activity);

    public void RemoveItem(Item item) => RemovedItems.Add(item);

    public void RemoveCost(Cost cost) => throw new NotSupportedException();

    public Task<int> SavedAsync()
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public async Task SaveEventChangesAsync(Guid eventId, Func<Event, Task> change)
    {
        await change(await GetEventByIdAsync(eventId));
        await SavedAsync();
    }
}
