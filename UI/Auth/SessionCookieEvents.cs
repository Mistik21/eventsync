using Microsoft.AspNetCore.Authentication.Cookies;

namespace eventsynс.UI.Auth;

public sealed class SessionCookieEvents : CookieAuthenticationEvents
{
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.TryGetCurrentUserId(out _) != true)
            context.RejectPrincipal();

        return Task.CompletedTask;
    }
}
