namespace eventsynс.UI.DTOs.Responses;

public record class BudgetResponse(
    int Budget,
    int CollectedBudget,
    int SpentAmount,
    int RemainingBudget,
    IReadOnlyDictionary<Guid, int> Debts,
    List<CostResponse> Costs
);
