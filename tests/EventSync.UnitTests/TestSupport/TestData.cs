using eventsynс.Domain.Entities;

namespace EventSync.UnitTests.TestSupport;

internal static class TestData
{
    public static DateTime EventStart { get; } = new(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    public static User CreateUser(long telegramId, string name = "Test user") =>
        new(Guid.NewGuid(), telegramId, name);

    public static Event CreateEvent(User organizer, params User[] participants) =>
        CreateEvent(organizer, EventStart, 0, participants);

    public static Event CreateEvent(User organizer, DateTime eventStart, int budget, params User[] participants)
    {
        var meetup = new Event(Guid.NewGuid(), "Meetup", "Event details", "https://example.test/cover.png",
            eventStart, new Location("Park", "56.84,60.61"), budget, organizer.Id);
        meetup.AddParticipant(organizer);
        foreach (var participant in participants)
        {
            meetup.AddParticipant(participant);
        }

        return meetup;
    }

    public static Cost SeedCost(Event meetup, int amount, string title = "Food")
    {
        var cost = new Cost(Guid.NewGuid(), title, amount);
        // Load an existing expense without applying the rules for creating a new one.
        ((ICollection<Cost>)meetup.Expenses).Add(cost);
        return cost;
    }

    public static Activity CreateActivity(User creator, DateTime start, int? limit = null) =>
        new(Guid.NewGuid(), "Board games", "Activity details", creator.Id, start, start.AddHours(1), limit);
}
