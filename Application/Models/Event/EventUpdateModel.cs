using eventsynс.Domain;

namespace eventsynс.Application.Models.Event;

public record class EventUpdateModel(
    string Title,
    string Description,
    DateTime EventStartTime,
    Location EventLocation,
    int Budget,
    string? MainImageUrl
);