using eventsynс.Domain.Entities;

namespace eventsynс.Application.Access;

internal static class EventAccess
{
    public static void RequireOrganizer(Event eventDetails, Guid currentUserId)
    {
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
    }

    public static void RequireParticipant(Event eventDetails, Guid currentUserId)
    {
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
    }
}
