using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}")]
public class CostsController : ControllerBase
{
    [HttpGet("budget")]
    public IActionResult GetBudget(Guid eventId) => Ok();

    [HttpPost("costs")]
    public IActionResult CreateCost(Guid eventId) => Ok();

    [HttpPatch("costs/{costId:guid}")]
    public IActionResult UpdateCost(Guid eventId, Guid costId) => Ok();

    [HttpDelete("costs/{costId:guid}")]
    public IActionResult DeleteCost(Guid eventId, Guid costId)
        => NoContent();
}