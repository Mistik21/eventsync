namespace eventsynс.UI.DTOs.Requests.Items;

public record UpdateItemRequest(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);