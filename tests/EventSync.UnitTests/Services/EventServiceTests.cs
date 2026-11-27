using eventsynс.Application.Models.Event;
using eventsynс.Application.Services.Events;
using eventsynс.Domain.Entities;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Services;

public class EventServiceTests
{
    private const string CoverDataUrl = "data:image/jpeg;base64,/9j/2Q==";
    private readonly User organizer = TestData.CreateUser(1, "Organizer");
    private readonly User attendee = TestData.CreateUser(2, "Attendee");
    private readonly InMemoryEventRepository events = new();
    private readonly InMemoryEventImageStorage imageStorage = new();

    [Theory]
    [InlineData(null)]
    [InlineData(CoverDataUrl)]
    public async Task CreateEvent_RegistersOrganizerAsParticipantAndSaves(string? cover)
    {
        var service = CreateService();
        var location = new Location("Park", "56.84,60.61");
        var data = new EventCreationModel("Picnic", "Bring food", TestData.EventStart, location, 120, cover);

        await service.CreateEventAsync(data, organizer.Id);

        var meetup = Assert.Single(events.Events);
        Assert.NotEqual(Guid.Empty, meetup.Id);
        Assert.True(meetup.IsOrganizer(organizer.Id));
        Assert.Same(organizer, Assert.Single(meetup.Participants));
        Assert.Equal(data.Title, meetup.Title);
        Assert.Equal(data.Description, meetup.Description);
        Assert.Equal(data.EventStartTime, meetup.EventStartTime);
        Assert.Same(location, meetup.EventLocation);
        Assert.Equal(data.Budget, meetup.Budget);
        Assert.Equal(1, events.SaveCount);
        if (cover is null)
        {
            Assert.Equal(string.Empty, meetup.MainImageUrl);
            Assert.Empty(imageStorage.Covers);
        }
        else
        {
            var uploaded = Assert.Single(imageStorage.Covers);
            Assert.Equal(uploaded.Key, meetup.MainImageUrl);
            Assert.StartsWith($"events/{meetup.Id}/covers/", uploaded.Key);
            Assert.Equal(cover, uploaded.Value);
        }
    }

    [Fact]
    public async Task CreateEvent_RejectsNegativeBudgetWithoutSaving()
    {
        var data = new EventCreationModel("Picnic", "Bring food", TestData.EventStart,
            new Location("Park", "56.84,60.61"), -1, CoverDataUrl);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateService().CreateEventAsync(data, organizer.Id));

        Assert.Empty(events.Events);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task GetEventAndParticipants_RequireMembership()
    {
        var meetup = AddEvent();
        var service = CreateService();

        Assert.Same(meetup, await service.GetEventAsync(meetup.Id, organizer.Id));
        Assert.Same(organizer, Assert.Single(await service.GetParticipantsAsync(meetup.Id, organizer.Id)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetEventAsync(meetup.Id, attendee.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetParticipantsAsync(meetup.Id, attendee.Id));
    }

    [Fact]
    public async Task GetMyEvents_IncludesOrganizedAndJoinedEvents()
    {
        var own = AddEvent();
        var joined = TestData.CreateEvent(attendee, organizer);
        events.AddEvent(joined);
        events.AddEvent(TestData.CreateEvent(attendee));

        var result = await CreateService().GetMyEventsAsync(organizer.Id);

        Assert.Equal(new[] { own.Id, joined.Id }.Order(), result.Select(meetup => meetup.Id).Order());
    }

    [Fact]
    public async Task GetInvitation_ReturnsEventAndOrganizerName()
    {
        var meetup = AddEvent();

        var invitation = await CreateService().GetInvitationAsync(meetup.Id);

        Assert.Same(meetup, invitation.Event);
        Assert.Equal(organizer.Name, invitation.OrganizerName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(CoverDataUrl)]
    public async Task UpdateEvent_UpdatesDetailsAndAppliesCoverChange(string? imageUrl)
    {
        var meetup = AddEvent();
        var oldCover = await imageStorage.UploadCoverAsync(meetup.Id, CoverDataUrl);
        meetup.ChangeMainImage(oldCover);
        var location = new Location("Lake", "55.1,61.1");
        var data = new EventUpdateModel("Updated title", "Updated description", TestData.EventStart.AddDays(2),
            location, 150, imageUrl);

        await CreateService().UpdateEventAsync(meetup.Id, data, organizer.Id);

        Assert.Equal(data.Title, meetup.Title);
        Assert.Equal(data.Description, meetup.Description);
        Assert.Equal(data.EventStartTime, meetup.EventStartTime);
        Assert.Same(location, meetup.EventLocation);
        Assert.Equal(data.Budget, meetup.Budget);
        Assert.Equal(1, events.SaveCount);
        if (imageUrl is null)
        {
            Assert.Equal(oldCover, meetup.MainImageUrl);
            Assert.Single(imageStorage.Covers);
        }
        else if (imageUrl.Length == 0)
        {
            Assert.Equal(string.Empty, meetup.MainImageUrl);
            Assert.Empty(imageStorage.Covers);
        }
        else
        {
            Assert.NotEqual(oldCover, meetup.MainImageUrl);
            Assert.Equal(meetup.MainImageUrl, Assert.Single(imageStorage.Covers).Key);
        }
    }

    [Fact]
    public async Task UpdateAndDeleteEvent_RejectNonOrganizerEvenIfParticipant()
    {
        var meetup = AddEvent(attendee);
        var service = CreateService();
        var originalTitle = meetup.Title;
        var data = new EventUpdateModel("Another title", "", TestData.EventStart, meetup.EventLocation, 0, CoverDataUrl);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateEventAsync(meetup.Id, data, attendee.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteEventAsync(meetup.Id, attendee.Id));

        Assert.Equal(originalTitle, meetup.Title);
        Assert.Same(meetup, Assert.Single(events.Events));
        Assert.Empty(imageStorage.Covers);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task DeleteEvent_OrganizerCanRemoveEvent()
    {
        var meetup = AddEvent();
        meetup.ChangeMainImage(await imageStorage.UploadCoverAsync(meetup.Id, CoverDataUrl));

        var deleted = await CreateService().DeleteEventAsync(meetup.Id, organizer.Id);

        Assert.True(deleted);
        Assert.Empty(events.Events);
        Assert.Empty(imageStorage.Covers);
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task JoinEvent_AddsUserAndRejectsDuplicateMembership()
    {
        var meetup = AddEvent();
        var service = CreateService();

        await service.JoinEventAsync(meetup.Id, attendee.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.JoinEventAsync(meetup.Id, attendee.Id));

        Assert.Equal(new[] { organizer, attendee }, meetup.Participants);
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task OpenCollection_OrganizerOpensCollection()
    {
        var meetup = AddEvent();
        Assert.False(meetup.IsOpenCollection());

        await CreateService().OpenCollectionAsync(meetup.Id, organizer.Id);

        Assert.True(meetup.IsOpenCollection());
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task OpenCollection_RejectsParticipantWhoIsNotOrganizer()
    {
        var meetup = AddEvent(attendee);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService().OpenCollectionAsync(meetup.Id, attendee.Id));

        Assert.False(meetup.IsOpenCollection());
        Assert.Empty(meetup.Debts);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task PayMyDebt_RecordsFullPaymentForOnlyTheCurrentParticipant()
    {
        var meetup = AddEvent(attendee);
        meetup.ChangeBudget(200);
        meetup.OpenCollection();
        var service = CreateService();

        await service.PayMyDebtAsync(meetup.Id, attendee.Id, 100);

        Assert.Equal(1, events.SaveCount);
        Assert.Equal(meetup.Id, Assert.Single(events.EventChangeRequests));
        Assert.Equal(100, meetup.CollectedBudget);
        Assert.Equal(100, meetup.Debts[organizer.Id]);
        Assert.Equal(0, meetup.Debts[attendee.Id]);
    }

    [Fact]
    public async Task PayMyDebt_PayingOriginalAmountAfterSupplementalCollectionLeavesTheExtraShare()
    {
        var meetup = AddStartedEventWithOverspend();
        var originalDebt = meetup.Debts[attendee.Id];
        var service = CreateService();
        await service.AddSupplementalDebtsAsync(meetup.Id, organizer.Id);

        await service.PayMyDebtAsync(meetup.Id, attendee.Id, originalDebt);

        Assert.Equal(50, meetup.CollectedBudget);
        Assert.Equal(10, meetup.Debts[attendee.Id]);
        Assert.Equal(60, meetup.Debts[organizer.Id]);

        await service.PayMyDebtAsync(meetup.Id, attendee.Id, 10);

        Assert.Equal(60, meetup.CollectedBudget);
        Assert.Equal(0, meetup.Debts[attendee.Id]);
        Assert.Equal(3, events.SaveCount);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(101)]
    public async Task PayMyDebt_RejectsInvalidAmountWithoutSaving(int amount)
    {
        var meetup = AddEvent(attendee);
        meetup.ChangeBudget(200);
        meetup.OpenCollection();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().PayMyDebtAsync(meetup.Id, attendee.Id, amount));

        Assert.Equal(0, meetup.CollectedBudget);
        Assert.Equal(100, meetup.Debts[attendee.Id]);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task PayMyDebt_RejectsOutsiderAndRejectsPaymentBeforeCollectionOpens()
    {
        var meetup = AddEvent(attendee);
        meetup.ChangeBudget(200);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayMyDebtAsync(meetup.Id, attendee.Id, 100));
        meetup.OpenCollection();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PayMyDebtAsync(meetup.Id, Guid.NewGuid(), 100));

        Assert.Equal(0, meetup.CollectedBudget);
        Assert.Equal(100, meetup.Debts[attendee.Id]);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task AddSupplementalDebts_UsesEventChangeAndSavesOnce()
    {
        var meetup = AddStartedEventWithOverspend();

        await CreateService().AddSupplementalDebtsAsync(meetup.Id, organizer.Id);

        Assert.Equal(120, meetup.Budget);
        Assert.Equal(60, meetup.Debts[organizer.Id]);
        Assert.Equal(60, meetup.Debts[attendee.Id]);
        Assert.Equal(meetup.Id, Assert.Single(events.EventChangeRequests));
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task AddSupplementalDebts_RejectsParticipantWhoIsNotOrganizer()
    {
        var meetup = AddStartedEventWithOverspend();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService().AddSupplementalDebtsAsync(meetup.Id, attendee.Id));

        Assert.Equal(100, meetup.Budget);
        Assert.Equal(50, meetup.Debts[attendee.Id]);
        Assert.Equal(0, events.SaveCount);
    }

    private Event AddStartedEventWithOverspend()
    {
        var meetup = TestData.CreateEvent(organizer, DateTime.UtcNow.AddDays(-1), 100, attendee);
        meetup.OpenCollection();
        TestData.SeedCost(meetup, 120);
        events.AddEvent(meetup);
        return meetup;
    }

    private EventService CreateService() => new(new InMemoryUserRepository(organizer, attendee), events, imageStorage);

    private Event AddEvent(params User[] participants)
    {
        var meetup = TestData.CreateEvent(organizer, participants);
        events.AddEvent(meetup);
        return meetup;
    }
}
