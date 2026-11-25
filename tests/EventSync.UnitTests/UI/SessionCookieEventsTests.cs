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
}
