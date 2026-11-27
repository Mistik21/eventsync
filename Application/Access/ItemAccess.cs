using eventsynс.Domain.Entities;

namespace eventsynс.Application.Access;

internal static class ItemAccess
{
    public static void RequireResponsible(Item item, Guid currentUserId)
    {
        if (!item.IsResponsible(currentUserId)) throw new UnauthorizedAccessException();
    }
}
