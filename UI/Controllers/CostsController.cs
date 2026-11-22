using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.Auth;
using eventsynс.Application.Models.Cost;
using eventsynс.UI.DTOs.Requests.Costs;
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
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await costService.GetBudgetAsync(eventId, currentUserId);
        return Ok(new BudgetResponse(
            result.Budget,
            result.CollectedBudget,
            result.SpentAmount,
            result.RemainingBudget,
            result.Debts,
            result.Costs.Select(cost => new CostResponse(cost.Id, cost.Title, cost.Amount)).ToList()));
    }

    [HttpPost("costs")]
    public async Task<IActionResult> CreateCost(CreateCostRequest data,Guid eventId)
    {
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        await costService.CreateCostAsync(eventId, new CostCreationModel(data.Title, data.Amount), currentUserId);
        return Ok();
    }

    [HttpPatch("costs/{costId:guid}")]
    public IActionResult UpdateCost(Guid eventId, Guid costId) => Ok();

    [HttpDelete("costs/{costId:guid}")]
    public IActionResult DeleteCost(Guid eventId, Guid costId)
        => NoContent();
}
