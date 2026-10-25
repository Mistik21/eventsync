namespace eventsynс.Application.Models.Activity;

public record class ActivityUpdateModel(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int MaxParticipants
);