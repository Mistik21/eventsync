using eventsynс.Application.Models;
using eventsynс.Application.Models.Cost;

namespace eventsynс.Application.Interfaces.Events;

public interface ICostService
{
    Task<BudgetData> GetBudgetAsync(Guid eventId, Guid currentUserId);

    Task CreateCostAsync(Guid eventId, CostCreationModel data, Guid currentUserId);

    Task UpdateCostAsync(Guid eventId, Guid costId, CostUpdateModel data, Guid currentUserId);

    Task DeleteCostAsync(Guid eventId, Guid costId, Guid currentUserId);
}