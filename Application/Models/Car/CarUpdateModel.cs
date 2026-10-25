using eventsynс.Domain;

namespace eventsynс.Application.Models.Car;

public record class CarUpdateModel(
    string Description,
    int MaxNumberSeats,
    Location LocationEnd
);