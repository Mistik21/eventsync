namespace eventsynс.Application.Models.Activity;

public record ActivityCreationModel(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int? MaxParticipants
);