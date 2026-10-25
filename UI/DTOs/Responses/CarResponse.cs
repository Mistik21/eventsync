namespace eventsynс.UI.DTOs.Responses;

public record class CarResponse(
    Guid Id,
    Guid DriverId,
    string DriverName,
    string Description,
    int MaxNumberSeats,
    int AvailableSeats,
    LocationResponse LocationEnd,
    int PassengersCount,
    bool IsPassenger
);