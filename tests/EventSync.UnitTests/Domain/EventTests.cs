using eventsynс.Domain.Entities;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class EventTests
{
    [Theory]
    [InlineData(90, 30, 30, 30)]
    [InlineData(100, 33, 33, 34)]
    [InlineData(2, 0, 1, 1)]
    public void OpenCollection_DistributesBudgetAmongParticipants(int budget, int firstDebt, int secondDebt, int thirdDebt)
    {
        var meetup = CreateEvent(budget);
        meetup.AddParticipant(new User(meetup.Organizer, 1, "Organizer"));
        meetup.AddParticipant(new User(Guid.NewGuid(), 2, "Second participant"));
        meetup.AddParticipant(new User(Guid.NewGuid(), 3, "Third participant"));

        meetup.OpenCollection();

        Assert.True(meetup.IsOpenCollection());
        Assert.Equal(new[] { firstDebt, secondDebt, thirdDebt }, meetup.Debts.Values.Order());
    }

    [Fact]
    public void PayDebt_ClearsOnlyThePaidDebt_AndCountsItOnce()
    {
        var meetup = CreateEvent(budget: 100);
        var participant = new User(Guid.NewGuid(), 2, "Participant");
        meetup.AddParticipant(new User(meetup.Organizer, 1, "Organizer"));
        meetup.AddParticipant(participant);
        meetup.OpenCollection();

        meetup.PayDebt(participant.Id);
        meetup.PayDebt(participant.Id);

        Assert.Equal(0, meetup.Debts[participant.Id]);
        Assert.Equal(50, meetup.Debts[meetup.Organizer]);
        Assert.Equal(50, meetup.CollectedBudget);
    }

    [Fact]
    public void ChangeBudget_RejectsChangesAfterCollectionOpens()
    {
        var meetup = CreateEvent(budget: 100);
        meetup.AddParticipant(new User(meetup.Organizer, 1, "Organizer"));
        meetup.OpenCollection();

        Assert.Throws<InvalidOperationException>(() => meetup.ChangeBudget(120));
        Assert.Equal(100, meetup.Budget);
    }

    [Fact]
    public void ChangeStartTime_KeepsActivityOffsets()
    {
        var meetup = CreateEvent(budget: 0);
        var activity = new Activity(
            Guid.NewGuid(), "Board games", "", meetup.Organizer,
            meetup.EventStartTime.AddHours(1), meetup.EventStartTime.AddHours(2), maxParticipants: null);
        meetup.AddActivity(activity);
        var newStart = meetup.EventStartTime.AddDays(1);

        meetup.ChangeStartTime(newStart);

        Assert.Equal(newStart, meetup.EventStartTime);
        Assert.Equal(newStart.AddHours(1), activity.АctivityStartTime);
        Assert.Equal(newStart.AddHours(2), activity.АctivityEndTime);
    }

    private static Event CreateEvent(int budget) => new(
        Guid.NewGuid(), "Meetup", "", mainImageUrl: null,
        new DateTime(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc),
        new Location("Test address", "56.84,60.61"), budget, Guid.NewGuid());
}
