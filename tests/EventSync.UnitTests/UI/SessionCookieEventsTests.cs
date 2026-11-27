using System.Security.Claims;
using eventsynс.UI.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EventSync.UnitTests.UI;

public class SessionCookieEventsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("invalid", false)]
    [InlineData("e0ad91d3-a821-4b53-9fca-5be30369c3fb", true)]
    public async Task ValidatePrincipal_RequiresValidUserId(string? userId, bool accepted)
    {
        Claim[] claims = userId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId)];
        const string schemeName = CookieAuthenticationDefaults.AuthenticationScheme;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, schemeName));
        var scheme = new AuthenticationScheme(schemeName, null, typeof(CookieAuthenticationHandler));
        var context = new CookieValidatePrincipalContext(
            new DefaultHttpContext(), scheme, new CookieAuthenticationOptions(),
            new AuthenticationTicket(principal, schemeName));

        await new SessionCookieEvents().ValidatePrincipal(context);

        Assert.Same(accepted ? principal : null, context.Principal);
    }

    [Theory]
    [InlineData(false, StatusCodes.Status401Unauthorized)]
    [InlineData(true, StatusCodes.Status403Forbidden)]
    public async Task RedirectEvents_ReturnStatusWithoutRedirect(bool accessDenied, int expectedStatus)
    {
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null,
            typeof(CookieAuthenticationHandler));
        var context = new RedirectContext<CookieAuthenticationOptions>(new DefaultHttpContext(), scheme,
            new CookieAuthenticationOptions(), new AuthenticationProperties(), "/login");
        var events = new SessionCookieEvents();

        await (accessDenied ? events.RedirectToAccessDenied(context) : events.RedirectToLogin(context));

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }
}
