namespace eventsynс.Application.Models.Activity;

public record class ActivityCreationModel(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int MaxParticipants
);