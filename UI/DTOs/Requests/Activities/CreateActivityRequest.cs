namespace UI.DTOs.Requests.Activities;

public record CreateActivityRequest(
    string Title,
    string Description,
    DateTime ActivityStartTime,
    DateTime ActivityEndTime,
    int MaxParticipants
);