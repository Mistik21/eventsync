using eventsynс.Domain;

namespace eventsynс.Application.Models.Car;

public record class CarCreationModel(
    string Description,
    int MaxNumberSeats,
    Location LocationEnd
);
