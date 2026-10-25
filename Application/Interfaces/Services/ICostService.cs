using eventsync.Application.Models;
using eventsynс.Application.Models;
using eventsynс.Application.Models.Cost;

namespace eventsynс.Application.Interfaces.Services;

public interface ICostService
{
    Task<BudgetData> GetBudgetAsync(Guid eventId, Guid currentUserId);

    Task<Guid> CreateCostAsync(Guid eventId, CostCreationModel data, Guid currentUserId);

    Task UpdateCostAsync(Guid costId, CostUpdateModel data, Guid currentUserId);

    Task DeleteCostAsync(Guid costId, Guid currentUserId);
}