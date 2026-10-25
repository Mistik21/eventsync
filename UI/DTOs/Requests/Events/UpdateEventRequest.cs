namespace UI.DTOs.Requests.Events;

public record UpdateEventRequest(
    string Title,
    string Description,
    DateTime EventStartTime,
    LocationRequest EventLocation,
    int Budget,
    string? MainImageUrl
);