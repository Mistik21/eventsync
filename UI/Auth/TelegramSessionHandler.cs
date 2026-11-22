using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using eventsynс.Application.Interfaces.Auth;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace eventsynс.UI.Auth;

public class TelegramSessionHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITelegramAuthValidator validator,
    IUserRepository users) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TelegramSession";
    public const string HeaderName = "X-EventSync-Telegram-Init-Data";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var values = Request.Headers[HeaderName];
        if (values.Count != 1 || values[0] is not { Length: > 0 } initData)
            return AuthenticateResult.Fail("Некорректные данные Telegram.");

        User user;
        try
        {
            var telegramUser = validator.Validate(initData);
            user = await users.GetByTelegramIdAsync(telegramUser.TelegramId);
        }
        catch (InvalidCredentialException)
        {
            return AuthenticateResult.Fail("Не удалось проверить данные Telegram.");
        }
        catch (KeyNotFoundException)
        {
            return AuthenticateResult.Fail("Пользователь Telegram не найден.");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
