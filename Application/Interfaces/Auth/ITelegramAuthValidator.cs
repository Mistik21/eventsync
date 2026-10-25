using eventsynс.Application.Models;
using eventsynс.Application.Models.Auth;

namespace eventsynс.Application.Interfaces.Auth;

public interface ITelegramAuthValidator
{
    Task<TelegramUserModel> ValidateAsync(string initData);
}