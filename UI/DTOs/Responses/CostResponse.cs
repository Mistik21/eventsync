namespace eventsynс.UI.DTOs.Responses;

public record class CostResponse(
    Guid Id,
    string Title,
    int Amount
);