using eventsynс.Domain;

namespace eventsynс.Application.Models.Event;

public record class EventCreationModel(
    string Title,
    string Description,
    DateTime EventStartTime,
    Location EventLocation,
    int Budget,
    string? MainImageUrl
);