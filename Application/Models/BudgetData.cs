namespace eventsynс.Application.Models;

public record BudgetData(
    int Budget,
    int CollectedBudget,
    int SpentAmount,
    int RemainingBudget,
    int MyDebt,
    List<Domain.Entities.Cost> Costs
);