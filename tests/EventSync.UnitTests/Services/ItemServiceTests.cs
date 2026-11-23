using eventsynс.Application.Models.Item;
using eventsynс.Application.Services;
using eventsynс.Domain.Entities;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Services;

public class ItemServiceTests
{
    private readonly User organizer = TestData.CreateUser(1);
    private readonly User attendee = TestData.CreateUser(2);
    private readonly Event meetup;
    private readonly InMemoryEventRepository events;
    private readonly ItemService service;

    public ItemServiceTests()
    {
        meetup = TestData.CreateEvent(organizer, attendee);
        events = new InMemoryEventRepository(meetup);
        service = new ItemService(events);
    }

    [Fact]
    public async Task CreateAndListItems_EventParticipantCanAddItem()
    {
        var data = new ItemCreationModel("Water", "One bottle per person", 4, "https://example.test/water");

        await service.CreateItemAsync(meetup.Id, data, attendee.Id);

        var item = Assert.Single(await service.GetItemsAsync(meetup.Id, organizer.Id));
        Assert.Equal(data.Title, item.Title);
        Assert.Equal(data.Description, item.Description);
        Assert.Equal(data.Count, item.Count);
        Assert.Equal(data.UrlShop, item.UrlShop);
        Assert.Null(item.ResponsibleID);
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task UpdateItem_UpdatesFieldsAndCanClearShopLink()
    {
        var item = AddItem();
        var data = new ItemUpdateModel("Sparkling water", "Large bottles", 2, null);

        await service.UpdateItemAsync(meetup.Id, item.Id, data, attendee.Id);

        Assert.Equal(data.Title, item.Title);
        Assert.Equal(data.Description, item.Description);
        Assert.Equal(data.Count, item.Count);
        Assert.Null(item.UrlShop);
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task DeleteItem_RemovesOnlyRequestedItem()
    {
        var removed = AddItem();
        var kept = AddItem();

        await service.DeleteItemAsync(meetup.Id, removed.Id, attendee.Id);

        Assert.Same(kept, Assert.Single(meetup.Items));
        Assert.Same(removed, Assert.Single(events.RemovedItems));
        Assert.Equal(1, events.SaveCount);
    }

    [Fact]
    public async Task AssignAndUnassign_OnlyResponsibleParticipantCanReleaseItem()
    {
        var item = AddItem();

        await service.AssignItemAsync(meetup.Id, item.Id, attendee.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignItemAsync(meetup.Id, item.Id, organizer.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UnassignItemAsync(meetup.Id, item.Id, organizer.Id));

        Assert.True(item.IsResponsible(attendee.Id));
        Assert.Equal(1, events.SaveCount);

        await service.UnassignItemAsync(meetup.Id, item.Id, attendee.Id);

        Assert.Null(item.ResponsibleID);
        Assert.Equal(2, events.SaveCount);
    }

    [Fact]
    public async Task ItemOperations_RejectUserOutsideEvent()
    {
        var item = AddItem();
        var outsiderId = Guid.NewGuid();
        var creation = new ItemCreationModel("Bread", "", 1, null);
        var update = new ItemUpdateModel("Bread", "", 1, null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetItemsAsync(meetup.Id, outsiderId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateItemAsync(meetup.Id, creation, outsiderId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateItemAsync(meetup.Id, item.Id, update, outsiderId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteItemAsync(meetup.Id, item.Id, outsiderId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AssignItemAsync(meetup.Id, item.Id, outsiderId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UnassignItemAsync(meetup.Id, item.Id, outsiderId));

        Assert.Same(item, Assert.Single(meetup.Items));
        Assert.Equal(0, events.SaveCount);
    }

    private Item AddItem()
    {
        var item = new Item(Guid.NewGuid(), "Still water", "", 1, "https://example.test/water");
        meetup.AddItem(item);
        return item;
    }
}
