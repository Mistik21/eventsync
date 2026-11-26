using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Models.Car;

public record CarCreationModel(
    string Description,
    int MaxNumberSeats,
    Location LocationEnd
);
