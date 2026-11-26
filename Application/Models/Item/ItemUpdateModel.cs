namespace eventsynс.Application.Models.Item;

public record ItemUpdateModel(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);