using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Models.Car;

public record CarUpdateModel(
    string Description,
    int MaxNumberSeats,
    Location LocationEnd
);