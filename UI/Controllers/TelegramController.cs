using System.Net;
using eventsynс.Application.Interfaces.Notifications;
using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly IUserService users;
    private readonly ITelegramNotificationService notifications;

    public TelegramController(IUserService users, ITelegramNotificationService notifications)
    {
        this.users = users;
        this.notifications = notifications;
    }

    [HttpPost("test-message")]
    public async Task<IActionResult> SendTestMessage()
    {
        var user = await users.GetMyProfileAsync(User.GetCurrentUserId());
        try
        {
            await notifications.SendMessageAsync(user.TelegramID, "Тестовое уведомление EventSync");
            return NoContent();
        }
        catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
        {
            return UnprocessableEntity();
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout);
        }
    }
}
