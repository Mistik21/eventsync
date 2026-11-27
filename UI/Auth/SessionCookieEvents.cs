using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace eventsynс.UI.Auth;

public class SessionCookieEvents : CookieAuthenticationEvents
{
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.TryGetCurrentUserId(out _) != true)
            context.RejectPrincipal();

        return Task.CompletedTask;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
