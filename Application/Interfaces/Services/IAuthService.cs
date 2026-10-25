using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User> AuthenticateTelegramAsync(string initData);
}