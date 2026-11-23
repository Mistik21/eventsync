using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Models.Auth;

namespace EventSync.UnitTests.TestSupport;

internal sealed class StubTelegramValidator(Func<string, TelegramUserModel> validate) : ITelegramAuthValidator
{
    public TelegramUserModel Validate(string initData) => validate(initData);
}
