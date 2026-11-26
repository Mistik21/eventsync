using System.Diagnostics;
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
    private readonly IHttpClientFactory httpClients;
    private readonly ILogger<TelegramController> logger;

    public TelegramController(IUserService users, ITelegramNotificationService notifications,
        IHttpClientFactory httpClients, ILogger<TelegramController> logger)
    {
        this.users = users;
        this.notifications = notifications;
        this.httpClients = httpClients;
        this.logger = logger;
    }

    [HttpGet("diagnostics")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CheckConnection(CancellationToken cancellationToken)
    {
        using var client = httpClients.CreateClient("TelegramDiagnostics");
        var results = await Task.WhenAll(
            ProbeAsync(client, new Uri("https://api.telegram.org/"), cancellationToken),
            ProbeAsync(client, new Uri("https://example.com/"), cancellationToken));
        return Ok(results);
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
        catch (HttpRequestException error)
        {
            logger.LogWarning("Telegram test message failed. HTTP status: {Status}; network error: {Error}.",
                (int?)error.StatusCode, error.HttpRequestError);

            return error.StatusCode switch
            {
                HttpStatusCode.BadRequest or HttpStatusCode.Forbidden => Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Откройте личный чат с ботом EventSync, нажмите «Старт» и попробуйте ещё раз."),
                HttpStatusCode.Unauthorized or HttpStatusCode.NotFound => Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: $"Telegram вернул HTTP {(int)error.StatusCode.Value}. Проверьте токен бота в настройках сервера."),
                HttpStatusCode.TooManyRequests => Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Telegram временно ограничил отправку. Попробуйте чуть позже."),
                null => Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Сервер EventSync не смог подключиться к Telegram."),
                _ => Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: $"Telegram вернул HTTP {(int)error.StatusCode.Value}.")
            };
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Telegram test message timed out.");
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Сервер EventSync не дождался ответа Telegram.");
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("Telegram test message could not be sent because the sender is not configured.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Отправка Telegram не настроена на сервере EventSync.");
        }
    }

    private static async Task<ConnectionProbe> ProbeAsync(HttpClient client, Uri address, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            using var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return new ConnectionProbe(address.Host, (int)response.StatusCode, null,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ConnectionProbe(address.Host, null, "Таймаут",
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (HttpRequestException exception)
        {
            var error = exception.HttpRequestError switch
            {
                HttpRequestError.NameResolutionError => "Ошибка DNS",
                HttpRequestError.ConnectionError => "Ошибка соединения",
                HttpRequestError.SecureConnectionError => "Ошибка TLS",
                _ => "Ошибка HTTPS"
            };
            return new ConnectionProbe(address.Host, null, error,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private sealed record ConnectionProbe(string Host, int? HttpStatus, string? Error, long ElapsedMs);
}
