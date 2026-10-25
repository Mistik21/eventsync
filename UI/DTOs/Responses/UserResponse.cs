namespace UI.DTOs.Responses;

public record class UserResponse(
    Guid Id,
    string Name,
    int AttendedEventCount
);