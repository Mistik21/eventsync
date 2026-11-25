using eventsynс.Application.Access;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Item;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class ItemService : IItemService
{
    private readonly IEventRepository eventRepository;

    public ItemService(IEventRepository eventRepository)
    {
        this.eventRepository = eventRepository;
    }

    public async Task<List<Item>> GetItemsAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        return eventDetails.Items.ToList();
    }

    public async Task CreateItemAsync(Guid eventId, ItemCreationModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        var item = new Item(
            Guid.Empty,
            data.Title,
            data.Description,
            data.Count,
            data.UrlShop
        );
        eventDetails.AddItem(item);
        await eventRepository.SavedAsync();
    }

    public async Task UpdateItemAsync(Guid eventId, Guid itemId, ItemUpdateModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        var item = eventDetails.GetRequiredItem(itemId);
        item.ChangeCount(data.Count);
        item.ChangeTitle(data.Title);
        item.ChangeDescription(data.Description);
        item.ChangeUrlShop(data.UrlShop);
        await eventRepository.SavedAsync();
    }

    public async Task DeleteItemAsync(Guid eventId, Guid itemId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        var item = eventDetails.GetRequiredItem(itemId);
        eventDetails.RemoveItem(itemId);
        eventRepository.RemoveItem(item);
        await eventRepository.SavedAsync();
    }

    public async Task AssignItemAsync(Guid eventId, Guid itemId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        var item = eventDetails.GetRequiredItem(itemId);
        item.AssignTo(currentUserId);
        await eventRepository.SavedAsync();
    }

    public async Task UnassignItemAsync(Guid eventId, Guid itemId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var item = eventDetails.GetRequiredItem(itemId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        ItemAccess.RequireResponsible(item, currentUserId);
        item.UnassignTo();
        await eventRepository.SavedAsync();
    }
}
