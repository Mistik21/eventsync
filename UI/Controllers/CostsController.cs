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
        var currentUserId = User.GetCurrentUserId();
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
        var currentUserId = User.GetCurrentUserId();
        await costService.CreateCostAsync(eventId, new CostCreationModel(data.Title, data.Amount), currentUserId);
        return Ok();
    }
    
    [HttpPatch("costs/{costId:guid}")]
    public async Task<IActionResult> UpdateCost(UpdateCostRequest data ,Guid eventId, Guid costId)
    {
        var currentUserId = User.GetCurrentUserId();
        await costService.UpdateCostAsync(eventId, costId, new CostUpdateModel(data.Amount), currentUserId);
        return Ok();
    }

    [HttpDelete("costs/{costId:guid}")]
    public async Task<IActionResult> DeleteCost(Guid eventId, Guid costId)
    {
        var currentUserId = User.GetCurrentUserId();
        await costService.DeleteCostAsync(eventId, costId, currentUserId);
        return Ok();
    }
}
