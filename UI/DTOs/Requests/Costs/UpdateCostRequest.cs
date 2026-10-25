namespace UI.DTOs.Requests.Costs;

public record UpdateCostRequest(
    string Title,
    int Amount
);