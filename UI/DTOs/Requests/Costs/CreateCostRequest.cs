namespace UI.DTOs.Requests.Costs;

public record CreateCostRequest(
    string Title,
    int Amount
);