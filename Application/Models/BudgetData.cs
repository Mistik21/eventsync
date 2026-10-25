using eventsynс.Domain;

namespace eventsync.Application.Models;

public record BudgetData(
    int Budget,
    int CollectedBudget,
    int SpentAmount,
    int RemainingBudget,
    int MyDebt,
    List<Cost> Costs
);