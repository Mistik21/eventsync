using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using eventsynс.UI.Auth;
using EventSync.UnitTests.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventSync.UnitTests.UI;

public class TelegramSessionHandlerTests
{
    [Fact]
    public async Task Authenticate_UsesLocalUserIdForValidTelegramSession()
    {
        var user = TestData.CreateUser(42);
        var validator = new StubTelegramValidator(data =>
        {
            Assert.Equal("signed-data", data);
            return new(42, "Telegram name");
        });

        var result = await AuthenticateAsync(validator, new InMemoryUserRepository(user), ["signed-data"]);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id.ToString(), result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal(TelegramSessionHandler.SchemeName, result.Ticket!.AuthenticationScheme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authenticate_RejectsInvalidCredentialsAndUnknownAccounts(bool invalidCredential)
    {
        var users = new InMemoryUserRepository();
        var validator = new StubTelegramValidator(_ => invalidCredential
            ? throw new InvalidCredentialException()
            : new(42, "Unknown user"));

        var result = await AuthenticateAsync(validator, users, ["init-data"]);

        Assert.NotNull(result.Failure);
        Assert.Null(result.Principal);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task Authenticate_RejectsRepeatedHeadersBeforeValidation()
    {
        var validator = new StubTelegramValidator(_ => throw new InvalidOperationException("Must reject duplicate headers first."));

        var result = await AuthenticateAsync(validator, new InMemoryUserRepository(), ["first", "second"]);

        Assert.NotNull(result.Failure);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        StubTelegramValidator validator, InMemoryUserRepository users, string[] header)
    {
        using var services = new ServiceCollection().AddOptions().BuildServiceProvider();
        var handler = new TelegramSessionHandler(services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
            NullLoggerFactory.Instance, UrlEncoder.Default, validator, users);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers[TelegramSessionHandler.HeaderName] = header;
        var scheme = new AuthenticationScheme(TelegramSessionHandler.SchemeName, null, typeof(TelegramSessionHandler));

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }
}
