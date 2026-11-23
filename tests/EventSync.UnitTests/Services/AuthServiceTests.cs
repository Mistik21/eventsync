using System.Security.Authentication;
using eventsynс.Application.Models.Auth;
using eventsynс.Application.Services;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task AuthenticateTelegram_ReusesExistingAccount()
    {
        var existing = TestData.CreateUser(42, "Existing name");
        var users = new InMemoryUserRepository(existing);
        var service = new AuthService(new StubTelegramValidator(_ => new(42, "Telegram name")), users);

        var result = await service.AuthenticateTelegramAsync("example-init-data");

        Assert.Same(existing, result);
        Assert.Same(existing, Assert.Single(users.Users));
        Assert.Equal(0, users.SaveCount);
    }

    [Fact]
    public async Task AuthenticateTelegram_CreatesAccountOnlyOnceForNewTelegramId()
    {
        const string initData = "example-init-data";
        var users = new InMemoryUserRepository(TestData.CreateUser(10));
        var service = new AuthService(new StubTelegramValidator(input =>
        {
            Assert.Equal(initData, input);
            return new TelegramUserModel(42, "New user");
        }), users);

        var created = await service.AuthenticateTelegramAsync(initData);
        var nextLogin = await service.AuthenticateTelegramAsync(initData);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(42, created.TelegramID);
        Assert.Equal("New user", created.Name);
        Assert.Same(created, nextLogin);
        Assert.Same(created, Assert.Single(users.Users, user => user.TelegramID == 42));
        Assert.Equal(1, users.SaveCount);
    }

    [Fact]
    public async Task AuthenticateTelegram_DoesNotCreateAccountForInvalidCredentials()
    {
        var users = new InMemoryUserRepository();
        var service = new AuthService(new StubTelegramValidator(_ => throw new InvalidCredentialException()), users);

        await Assert.ThrowsAsync<InvalidCredentialException>(() => service.AuthenticateTelegramAsync("invalid"));

        Assert.Empty(users.Users);
        Assert.Equal(0, users.SaveCount);
    }
}
