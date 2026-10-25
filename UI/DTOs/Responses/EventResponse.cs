using Domain;

namespace UI.DTOs.Responses;

public record class EventResponse(
    Guid Id,
    string Title,
    string Description,
    string? MainImageUrl,
    DateTime EventStartTime,
    LocationResponse EventLocation,
    int Budget,
    int RemainingBudget,
    Guid OrganizerId,
    int ParticipantsCount,
    bool IsOrganizer,
    bool IsParticipant,
    List<string> ImageEventUrls,
    CollectionStatus CollectionStatus
);