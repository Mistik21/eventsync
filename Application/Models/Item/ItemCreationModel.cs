namespace eventsynс.Application.Models.Item;

public record class ItemCreationModel(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);