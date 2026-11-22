using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class AuthService : IAuthService
{
    private readonly ITelegramAuthValidator telegramAuthValidator;
    private readonly IUserRepository userRepository;

    public AuthService(ITelegramAuthValidator telegramAuthValidator, IUserRepository userRepository)
    {
        this.telegramAuthValidator = telegramAuthValidator;
        this.userRepository = userRepository;
    }

    public async Task<User> AuthenticateTelegramAsync(string initData)
    {
        var telegramUser = telegramAuthValidator.Validate(initData);

        try
        {
            var user = await userRepository.GetByTelegramIdAsync(telegramUser.TelegramId);
            return user;
        }
        catch (KeyNotFoundException)
        {
            var user = new User(Guid.NewGuid(), telegramUser.TelegramId, telegramUser.Name, 0);

            userRepository.Add(user);
            await userRepository.SavedAsync();
            return user;
        }
    }
}
