using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/events/{eventId:guid}")]
public class CostsController : ControllerBase
{
    private readonly ICostService costService;

    public CostsController(ICostService costService)
    {
        this.costService = costService;
    }

    [HttpGet("budget")]
    public async Task<IActionResult> GetBudget(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await costService.GetBudgetAsync(eventId, currentUserId);
            return Ok(new BudgetResponse(
                result.Budget,
                result.CollectedBudget,
                result.SpentAmount,
                result.RemainingBudget,
                result.Debts,
                result.Costs.Select(a=>new CostResponse(a.Id,a.Title,a.Amount)).ToList()
                ));
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPost("costs")]
    public IActionResult CreateCost(Guid eventId) => Ok();

    [HttpPatch("costs/{costId:guid}")]
    public IActionResult UpdateCost(Guid eventId, Guid costId) => Ok();

    [HttpDelete("costs/{costId:guid}")]
    public IActionResult DeleteCost(Guid eventId, Guid costId)
        => NoContent();
}