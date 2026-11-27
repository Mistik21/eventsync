using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<User> AuthenticateTelegramAsync(string initData);
}