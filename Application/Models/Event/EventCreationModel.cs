using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Models.Event;

public record class EventCreationModel(
    string Title,
    string Description,
    DateTime EventStartTime,
    Location EventLocation,
    int Budget,
    string? MainImageUrl
);