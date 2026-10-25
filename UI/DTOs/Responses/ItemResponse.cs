namespace UI.DTOs.Responses;

public record class ItemResponse(
    Guid Id,
    string Title,
    string Description,
    int Count,
    Guid? ResponsibleId,
    string? UrlShop
);