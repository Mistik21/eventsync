using EventSync.UnitTests.TestSupport;
using eventsynс.Domain.Entities;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class ActivityTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Create_RejectsNonPositiveLimit(int limit)
    {
        var creator = TestData.CreateUser(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TestData.CreateActivity(creator, TestData.EventStart, limit));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Create_RejectsEndAtOrBeforeStart(int durationMinutes)
    {
        var creator = TestData.CreateUser(1);
        var start = TestData.EventStart;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Activity(Guid.NewGuid(), "Board games", "", creator.Id, start, start.AddMinutes(durationMinutes), null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ChangeTime_RejectsEndAtOrBeforeStartAndKeepsPreviousTimes(int durationMinutes)
    {
        var activity = TestData.CreateActivity(TestData.CreateUser(1), TestData.EventStart);
        var newStart = TestData.EventStart.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            activity.ChangeTime(newStart, newStart.AddMinutes(durationMinutes)));

        Assert.Equal(TestData.EventStart, activity.АctivityStartTime);
        Assert.Equal(TestData.EventStart.AddHours(1), activity.АctivityEndTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ChangeMaxParticipants_RejectsInvalidLimitAndKeepsPreviousLimit(int limit)
    {
        var creator = TestData.CreateUser(1);
        var activity = TestData.CreateActivity(creator, TestData.EventStart, limit: 3);
        activity.AddParticipant(creator);
        activity.AddParticipant(TestData.CreateUser(2));

        Assert.Throws<ArgumentOutOfRangeException>(() => activity.ChangeMaxParticipants(limit));

        Assert.Equal(3, activity.MaxParticipants);
        Assert.True(activity.HasAvailableSeats());
    }

    [Fact]
    public void ChangeMaxParticipants_NullRemovesLimit()
    {
        var creator = TestData.CreateUser(1);
        var activity = TestData.CreateActivity(creator, TestData.EventStart, limit: 1);
        activity.AddParticipant(creator);
        Assert.False(activity.HasAvailableSeats());

        activity.ChangeMaxParticipants(null);
        activity.AddParticipant(TestData.CreateUser(2));

        Assert.Equal(2, activity.Participants.Count);
        Assert.True(activity.HasAvailableSeats());
    }

    [Fact]
    public void AddParticipant_RejectsDuplicateAndFullActivity()
    {
        var creator = TestData.CreateUser(1);
        var participant = TestData.CreateUser(2);
        var activity = TestData.CreateActivity(creator, TestData.EventStart, limit: 2);
        activity.AddParticipant(creator);

        Assert.Throws<InvalidOperationException>(() => activity.AddParticipant(creator));
        Assert.True(activity.HasAvailableSeats());
        activity.AddParticipant(participant);

        Assert.Throws<InvalidOperationException>(() => activity.AddParticipant(TestData.CreateUser(3)));
        Assert.Equal(new[] { creator, participant }, activity.Participants);
        Assert.False(activity.HasAvailableSeats());
    }

    [Fact]
    public void RemoveParticipant_ReleasesSeatAndKeepsCreatorEnrolled()
    {
        var creator = TestData.CreateUser(1);
        var participant = TestData.CreateUser(2);
        var activity = TestData.CreateActivity(creator, TestData.EventStart, limit: 2);
        activity.AddParticipant(creator);
        activity.AddParticipant(participant);

        Assert.Throws<InvalidOperationException>(() => activity.RemoveParticipant(creator.Id));
        activity.RemoveParticipant(participant.Id);

        Assert.Throws<InvalidOperationException>(() => activity.RemoveParticipant(participant.Id));
        Assert.Same(creator, Assert.Single(activity.Participants));
        Assert.True(activity.HasAvailableSeats());
    }
}
