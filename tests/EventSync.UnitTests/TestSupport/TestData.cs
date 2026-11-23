using eventsynс.Domain.Entities;

namespace EventSync.UnitTests.TestSupport;

internal static class TestData
{
    public static DateTime EventStart { get; } = new(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    public static User CreateUser(long telegramId, string name = "Test user") =>
        new(Guid.NewGuid(), telegramId, name);

    public static Event CreateEvent(User organizer, params User[] participants)
    {
        var meetup = new Event(Guid.NewGuid(), "Meetup", "Event details", "https://example.test/cover.png",
            EventStart, new Location("Park", "56.84,60.61"), budget: 0, organizer.Id);
        meetup.AddParticipant(organizer);
        foreach (var participant in participants)
        {
            meetup.AddParticipant(participant);
        }

        return meetup;
    }

    public static Activity CreateActivity(User creator, DateTime start, int? limit = null) =>
        new(Guid.NewGuid(), "Board games", "Activity details", creator.Id, start, start.AddHours(1), limit);
}
