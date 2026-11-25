using eventsynс.Domain.Entities;

namespace eventsynс.Application.Access;

internal static class ActivityAccess
{
    public static void RequireCreator(Activity activity, Guid currentUserId)
    {
        if (!activity.IsCreator(currentUserId))
            throw new UnauthorizedAccessException();
    }
}
