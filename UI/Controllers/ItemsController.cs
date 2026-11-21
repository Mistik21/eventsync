using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
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
    public async Task<IActionResult> GetItems(Guid eventId){if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result= await itemService.GetItemsAsync(eventId, currentUserId);
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest();
        }
        
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(CreateItemRequest data ,Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await itemService.CreateItemAsync(
                eventId,
                new ItemCreationModel(
                    data.Title,
                    data.Description,
                    data.Count,
                    data.UrlShop
                    ),
                currentUserId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPatch("{itemId:guid}")]
    public IActionResult UpdateItem(Guid eventId, Guid itemId) => Ok();

    [HttpDelete("{itemId:guid}")]
    public IActionResult DeleteItem(Guid eventId, Guid itemId) => NoContent();

    [HttpPost("{itemId:guid}/assign")]
    public IActionResult AssignItem(Guid eventId, Guid itemId) => Ok();

    [HttpPost("{itemId:guid}/unassign")]
    public IActionResult UnassignItem(Guid eventId, Guid itemId) => NoContent();
}