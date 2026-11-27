using System.Security.Cryptography;
using System.Text;
using eventsynс.Application.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramNotificationService notificationsService;
    private readonly IConfiguration configuration;

    public TelegramController(ITelegramNotificationService notificationsService, IConfiguration configuration)
    {
        this.notificationsService = notificationsService;
        this.configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("payment-reminders")]
    public async Task<IActionResult> SendPaymentReminders()
    {
        var secret = configuration["Telegram:ReminderSecret"]?.Trim();
        if (string.IsNullOrEmpty(secret) || Request.Headers["X-EventSync-Reminder-Secret"] is not [string providedSecret] ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(providedSecret)))
            return Unauthorized();

        await notificationsService.SendPaymentRemindersAsync();
        return NoContent();
    }
}
