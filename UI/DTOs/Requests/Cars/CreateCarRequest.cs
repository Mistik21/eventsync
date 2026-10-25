namespace eventsynс.UI.DTOs.Requests.Cars;

public record CreateCarRequest(
    string Description,
    int MaxNumberSeats,
    LocationRequest LocationEnd
);