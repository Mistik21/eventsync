namespace eventsynс.UI.DTOs.Requests.Items;

public record CreateItemRequest(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);