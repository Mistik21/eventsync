namespace eventsynс.Application.Models.Item;

public record class ItemUpdateModel(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);