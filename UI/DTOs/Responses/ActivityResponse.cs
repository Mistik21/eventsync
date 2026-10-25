namespace eventsynс.UI.DTOs.Responses;

public record class ActivityResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CreatorId,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int MaxParticipants,
    int ParticipantsCount,
    bool IsParticipant
);