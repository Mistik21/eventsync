using eventsynс.Application.Interfaces.Notifications;
using eventsynс.UI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.UI;

public class TelegramControllerTests
{
    [Fact]
    public async Task SendPaymentReminders_InvokesServiceWithValidSecret()
    {
        var notifications = new RecordingNotifications();
        var controller = CreateController(notifications, "reminder-secret", "reminder-secret");

        Assert.IsType<NoContentResult>(await controller.SendPaymentReminders());
        Assert.Equal(1, notifications.SendCount);
    }

    [Theory]
    [InlineData("reminder-secret", null)]
    [InlineData("reminder-secret", "")]
    [InlineData("reminder-secret", "wrong-secret")]
    [InlineData("reminder-secret", "Bearer reminder-secret")]
    [InlineData(null, "reminder-secret")]
    public async Task SendPaymentReminders_RejectsUnauthorizedRequests(string? secret, string? providedSecret)
    {
        var notifications = new RecordingNotifications();
        var controller = CreateController(notifications, secret, providedSecret);

        Assert.IsType<UnauthorizedResult>(await controller.SendPaymentReminders());
        Assert.Equal(0, notifications.SendCount);
    }

    private static TelegramController CreateController(
        RecordingNotifications notifications, string? secret, string? providedSecret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Telegram:ReminderSecret"] = secret })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-EventSync-Reminder-Secret"] = providedSecret;

        return new TelegramController(notifications, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class RecordingNotifications : ITelegramNotificationService
    {
        public int SendCount { get; private set; }

        public Task SendPaymentRemindersAsync()
        {
            SendCount++;
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(long telegramId, string message) => throw new NotSupportedException();
    }
}
