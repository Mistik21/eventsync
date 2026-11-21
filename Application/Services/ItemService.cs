using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Item;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class ItemService:IItemService
{
    private readonly IEventRepository eventRepository;

    public ItemService(IEventRepository eventRepository)
    {
        this.eventRepository = eventRepository;
    }
    
    public async Task<List<Item>> GetItemsAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        if (eventDetails == null || !eventDetails.IsParticipant(currentUserId))
        {
            throw new Exception();
        }
        return eventDetails.Items.ToList();
    }

    public async Task CreateItemAsync(Guid eventId, ItemCreationModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        if (eventDetails == null || !eventDetails.IsParticipant(currentUserId) || data.Count<0)
        {
            throw new Exception();
        }

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

    public Task UpdateItemAsync(Guid itemId, ItemUpdateModel data, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteItemAsync(Guid itemId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task AssignItemAsync(Guid eventId ,Guid itemId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        var item = eventDetails?.Items.SingleOrDefault(i => i.Id == itemId);
        if (eventDetails == null || !eventDetails.IsParticipant(currentUserId) || item==null || item.ResponsibleID!=null)
        {
            throw new Exception();
        }
        item.AssignTo(currentUserId);
        await eventRepository.SavedAsync();
    }

    public async Task UnassignItemAsync(Guid eventId, Guid itemId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetByIdAsync(eventId);
        var item = eventDetails?.Items.SingleOrDefault(i => i.Id == itemId);
        if (eventDetails == null || !eventDetails.IsParticipant(currentUserId) || item==null || !item.IsResponsible(currentUserId))
        {
            throw new Exception();
        }
        item.UnassignTo();
        await eventRepository.SavedAsync();
    }
}
