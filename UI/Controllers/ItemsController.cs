using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/items")]
public class ItemsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetItems(Guid eventId) => Ok();

    [HttpPost]
    public IActionResult CreateItem(Guid eventId) => Ok();

    [HttpPatch("{itemId:guid}")]
    public IActionResult UpdateItem(Guid eventId, Guid itemId) => Ok();

    [HttpDelete("{itemId:guid}")]
    public IActionResult DeleteItem(Guid eventId, Guid itemId) => NoContent();

    [HttpPost("{itemId:guid}/assign")]
    public IActionResult AssignItem(Guid eventId, Guid itemId) => Ok();

    [HttpPost("{itemId:guid}/unassign")]
    public IActionResult UnassignItem(Guid eventId, Guid itemId) => NoContent();
}