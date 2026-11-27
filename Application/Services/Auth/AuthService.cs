using eventsynс.Application.Interfaces.Auth;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories.Interfaces;

namespace eventsynс.Application.Services.Auth;

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
            var user = new User(Guid.NewGuid(), telegramUser.TelegramId, telegramUser.Name);

            userRepository.Add(user);
            await userRepository.SavedAsync();
            return user;
        }
    }
}
