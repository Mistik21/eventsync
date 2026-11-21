using eventsynс.Application.Models;
using eventsynс.Application.Models.Item;
using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Interfaces.Services;

public interface IItemService
{
    Task<List<Item>> GetItemsAsync(Guid eventId, Guid currentUserId);

    Task CreateItemAsync(Guid eventId, ItemCreationModel data, Guid currentUserId);

    Task UpdateItemAsync(Guid eventId, Guid itemId, ItemUpdateModel data, Guid currentUserId);

    Task DeleteItemAsync(Guid eventId, Guid itemId, Guid currentUserId);

    Task AssignItemAsync(Guid eventId ,Guid itemId, Guid currentUserId);
    Task UnassignItemAsync(Guid eventId, Guid itemId, Guid currentUserId);
}