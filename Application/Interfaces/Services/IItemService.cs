using eventsynс.Application.Models;
using eventsynс.Application.Models.Item;
using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Services;

public interface IItemService
{
    Task<List<Item>> GetItemsAsync(Guid eventId, Guid currentUserId);

    Task<Guid> CreateItemAsync(Guid eventId, ItemCreationModel data, Guid currentUserId);

    Task UpdateItemAsync(Guid itemId, ItemUpdateModel data, Guid currentUserId);

    Task DeleteItemAsync(Guid itemId, Guid currentUserId);

    Task AssignItemAsync(Guid itemId, Guid currentUserId);
    Task UnassignItemAsync(Guid itemId, Guid currentUserId);
}