namespace UI.DTOs.Requests;

public record LocationRequest(
    string Address,
    string Coordinates
);