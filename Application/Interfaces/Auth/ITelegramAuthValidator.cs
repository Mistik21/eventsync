using eventsynс.Application.Models.Auth;

namespace eventsynс.Application.Interfaces.Auth;

public interface ITelegramAuthValidator
{
    TelegramUserModel Validate(string initData);
}
