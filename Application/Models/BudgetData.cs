namespace eventsynс.Application.Models;

public record BudgetData(
    int Budget,
    int CollectedBudget,
    int SpentAmount,
    int RemainingBudget,
    IReadOnlyDictionary<Guid, int> Debts,
    List<Domain.Entities.Cost> Costs
);