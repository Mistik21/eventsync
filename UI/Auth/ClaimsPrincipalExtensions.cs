using System.Security.Claims;

namespace eventsynс.UI.Auth;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetCurrentUserId(this ClaimsPrincipal user, out Guid userId)
    {
        return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
