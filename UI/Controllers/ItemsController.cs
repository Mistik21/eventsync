using eventsynс.Application.Interfaces.Events;
using eventsynс.UI.Auth;
using eventsynс.Application.Models.Item;
using eventsynс.UI.DTOs.Requests.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/events/{eventId:guid}/items")]
public class ItemsController : ControllerBase
{
    
    private readonly IItemService itemService;

    public ItemsController(IItemService itemService)
    {
        this.itemService = itemService;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        var result = await itemService.GetItemsAsync(eventId, currentUserId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(CreateItemRequest data ,Guid eventId)
    {
        var currentUserId = User.GetCurrentUserId();
        await itemService.CreateItemAsync(
            eventId,
            new ItemCreationModel(data.Title, data.Description, data.Count, data.UrlShop),
            currentUserId);
        return Ok();
    }

    [HttpPatch("{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(UpdateItemRequest data,Guid eventId, Guid itemId)
    {
        var currentUserId = User.GetCurrentUserId();
        await itemService.UpdateItemAsync(
            eventId,
            itemId,
            new ItemUpdateModel(data.Title, data.Description, data.Count, data.UrlShop),
            currentUserId);
        return Ok();
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid eventId, Guid itemId)
    {
        var currentUserId = User.GetCurrentUserId();
        await itemService.DeleteItemAsync(eventId, itemId, currentUserId);
        return Ok();
    }

    [HttpPost("{itemId:guid}/assign")]
    public async Task<IActionResult> AssignItem(Guid eventId, Guid itemId)
    {
        var currentUserId = User.GetCurrentUserId();
        await itemService.AssignItemAsync(eventId, itemId, currentUserId);
        return Ok();
    }

    [HttpPost("{itemId:guid}/unassign")]
    public async Task<IActionResult> UnassignItem(Guid eventId, Guid itemId)
    {
        var currentUserId = User.GetCurrentUserId();
        await itemService.UnassignItemAsync(eventId, itemId, currentUserId);
        return Ok();
    }
}
