using eventsynс.Application.Models.Activity;
using eventsynс.Application.Services;
using eventsynс.Domain.Entities;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Services;

public class ActivityServiceTests
{
    private readonly User organizer = TestData.CreateUser(1);
    private readonly User attendee = TestData.CreateUser(2);
    private readonly User outsider = TestData.CreateUser(3);
    private readonly Event meetup;
    private readonly InMemoryEventRepository events;
    private readonly ActivityService service;

    public ActivityServiceTests()
    {
        meetup = TestData.CreateEvent(organizer, attendee);
        events = new InMemoryEventRepository(meetup);
        service = new ActivityService(events, new InMemoryUserRepository(organizer, attendee, outsider));
    }

    [Fact]
    public async Task CreateActivity_AddsCreatorToParticipantList()
    {
        var data = new ActivityCreationModel("Board games", "Bring a game",
            meetup.EventStartTime, meetup.EventStartTime.AddHours(1), 4);

        await service.CreateActivityAsync(meetup.Id, data, attendee.Id);

        var activity = Assert.Single(await service.GetActivitiesAsync(meetup.Id, organizer.Id));
        Assert.True(activity.IsCreator(attendee.Id));
        Assert.Same(attendee, Assert.Single(activity.Participants));
        Assert.Equal(data.Title, activity.Title);
        Assert.Equal(data.Description, activity.Description);
        Assert.Equal(data.ActivityStartTime, activity.АctivityStartTime);
        Assert.Equal(data.ActivityEndTime, activity.АctivityEndTime);
        Assert.Equal(data.MaxParticipants, activity.MaxParticipants);
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task CreateActivity_RejectsStartBeforeEvent()
    {
        var data = new ActivityCreationModel("Board games", "", meetup.EventStartTime.AddMinutes(-1),
            meetup.EventStartTime.AddHours(1), null);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateActivityAsync(meetup.Id, data, attendee.Id));

        Assert.Empty(meetup.Activities);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task UpdateActivity_CreatorCanChangeDetails()
    {
        var activity = AddActivity(attendee);
        var data = new ActivityUpdateModel("New title", "New description",
            meetup.EventStartTime.AddHours(2), meetup.EventStartTime.AddHours(3), 2);

        await service.UpdateActivityAsync(meetup.Id, activity.Id, data, attendee.Id);

        Assert.Equal(data.Title, activity.Title);
        Assert.Equal(data.Description, activity.Description);
        Assert.Equal(data.ActivityStartTime, activity.АctivityStartTime);
        Assert.Equal(data.ActivityEndTime, activity.АctivityEndTime);
        Assert.Equal(data.MaxParticipants, activity.MaxParticipants);
        Assert.Same(attendee, Assert.Single(activity.Participants));
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task UpdateActivity_RejectsStartBeforeEventWithoutChangingDetails()
    {
        var activity = AddActivity(attendee);
        var originalTitle = activity.Title;
        var data = new ActivityUpdateModel("New title", "", meetup.EventStartTime.AddMinutes(-1),
            meetup.EventStartTime.AddHours(1), null);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateActivityAsync(meetup.Id, activity.Id, data, attendee.Id));

        Assert.Equal(originalTitle, activity.Title);
        Assert.Equal(meetup.EventStartTime, activity.АctivityStartTime);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task UpdateAndDeleteActivity_RejectEventOrganizerWhenNotActivityCreator()
    {
        var activity = AddActivity(attendee);
        var title = activity.Title;
        var data = new ActivityUpdateModel("New title", "", meetup.EventStartTime, meetup.EventStartTime.AddHours(1), null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateActivityAsync(meetup.Id, activity.Id, data, organizer.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteActivityAsync(meetup.Id, activity.Id, organizer.Id));

        Assert.Equal(title, activity.Title);
        Assert.Same(activity, Assert.Single(meetup.Activities));
        Assert.Empty(events.RemovedActivities);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task DeleteActivity_RemovesOnlyRequestedActivity()
    {
        var removed = AddActivity(attendee);
        var kept = AddActivity(attendee);

        await service.DeleteActivityAsync(meetup.Id, removed.Id, attendee.Id);

        Assert.Same(kept, Assert.Single(meetup.Activities));
        Assert.Same(removed, Assert.Single(events.RemovedActivities));
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task JoinAndLeaveActivity_ChangeMembershipAndAvailability()
    {
        var activity = AddActivity(organizer, limit: 2);

        await service.JoinActivityAsync(meetup.Id, activity.Id, attendee.Id);

        Assert.True(activity.IsParticipant(attendee.Id));
        Assert.False(activity.HasAvailableSeats());

        await service.LeaveActivityAsync(meetup.Id, activity.Id, attendee.Id);

        Assert.Same(organizer, Assert.Single(activity.Participants));
        Assert.True(activity.HasAvailableSeats());
        Assert.Equal(2, events.SaveCount);
    }

    [Fact]
    public async Task ActivityOperations_RejectUserOutsideEvent()
    {
        var activity = AddActivity(organizer);
        var start = meetup.EventStartTime;
        var creation = new ActivityCreationModel("Board games", "", start, start.AddHours(1), null);
        var update = new ActivityUpdateModel("New title", "", start, start.AddHours(1), null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetActivitiesAsync(meetup.Id, outsider.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateActivityAsync(meetup.Id, creation, outsider.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateActivityAsync(meetup.Id, activity.Id, update, outsider.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteActivityAsync(meetup.Id, activity.Id, outsider.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.JoinActivityAsync(meetup.Id, activity.Id, outsider.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LeaveActivityAsync(meetup.Id, activity.Id, outsider.Id));

        Assert.Same(activity, Assert.Single(meetup.Activities));
        Assert.Equal(0, events.SaveCount);
    }

    private Activity AddActivity(User creator, int? limit = null)
    {
        var activity = TestData.CreateActivity(creator, meetup.EventStartTime, limit);
        activity.AddParticipant(creator);
        meetup.AddActivity(activity);
        return activity;
    }
}
