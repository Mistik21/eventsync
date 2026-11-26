namespace eventsynс.Application.Models.Activity;

public record  ActivityUpdateModel(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int? MaxParticipants
);