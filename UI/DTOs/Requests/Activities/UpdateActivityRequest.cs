namespace eventsynс.UI.DTOs.Requests.Activities;

public record UpdateActivityRequest(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int MaxParticipants
);