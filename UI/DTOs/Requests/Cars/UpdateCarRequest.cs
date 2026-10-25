namespace UI.DTOs.Requests.Cars;

public record UpdateCarRequest(
    string Description,
    int MaxNumberSeats,
    LocationRequest LocationEnd
);