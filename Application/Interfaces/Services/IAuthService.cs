using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User> AuthenticateTelegramAsync(string initData);
}