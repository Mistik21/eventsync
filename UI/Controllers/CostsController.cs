using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
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
    public async Task<IActionResult> CreateCost(CreateCostRequest data,Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            await costService.CreateCostAsync(eventId,new CostCreationModel(data.Title,data.Amount), currentUserId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpPatch("costs/{costId:guid}")]
    public IActionResult UpdateCost(Guid eventId, Guid costId) => Ok();

    [HttpDelete("costs/{costId:guid}")]
    public IActionResult DeleteCost(Guid eventId, Guid costId)
        => NoContent();
}