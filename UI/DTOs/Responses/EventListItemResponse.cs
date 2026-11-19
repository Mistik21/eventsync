namespace eventsynс.UI.DTOs.Responses;

public record EventListItemResponse(
    Guid Id,
    string Title,
    string Description,
    string? MainImageUrl,
    DateTime EventStartTime,
    LocationResponse EventLocation,
    bool IsOrganizer
);
