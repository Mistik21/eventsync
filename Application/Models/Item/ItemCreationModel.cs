namespace eventsynс.Application.Models.Item;

public record ItemCreationModel(
    string Title,
    string Description,
    int Count,
    string? UrlShop
);