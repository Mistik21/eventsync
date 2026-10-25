namespace UI.DTOs.Responses;

public class BudgetResponse(
    int Budget,
    int CollectedBudget,
    int SpentAmount,
    int RemainingBudget,
    int MyDebt,
    List<CostResponse> Costs
);