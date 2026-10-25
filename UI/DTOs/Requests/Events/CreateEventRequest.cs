using eventsynс.Domain;

namespace eventsynс.UI.DTOs.Requests.Events;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime EventStartTime,
    LocationRequest EventLocation,
    int Budget,
    string? MainImageUrl,
    CollectionStatus CollectionStatus
);