using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using eventsynс.Application.Interfaces.Notifications;

namespace eventsynс.Application.Services;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly HttpClient client;
    private readonly IConfiguration configuration;

    public TelegramNotificationService(HttpClient client, IConfiguration configuration)
    {
        this.client = client;
        this.configuration = configuration;
    }

    public async Task SendMessageAsync(long telegramId, string message)
    {
        var relayUrl = configuration["Telegram:RelayUrl"]?.Trim();
        var relaySecret = configuration["Telegram:RelaySecret"]?.Trim();
        if (!string.IsNullOrEmpty(relayUrl) || !string.IsNullOrEmpty(relaySecret))
        {
            await SendViaRelayAsync(relayUrl, relaySecret, telegramId, message);
            return;
        }

        var token = configuration["Telegram:BotToken"]?.Trim();
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException();

        using var response = await client.PostAsJsonAsync(
            $"/bot{token}/sendMessage",
            new { chat_id = telegramId, text = message });
        response.EnsureSuccessStatusCode();
    }

    private async Task SendViaRelayAsync(string? url, string? secret, long telegramId, string message)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("Настройте HTTPS-адрес и секрет сервиса отправки Telegram.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, address)
        {
            Content = JsonContent.Create(new { chatId = telegramId, text = message })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.NoContent)
            return;

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or
            HttpStatusCode.ServiceUnavailable)
        {
            throw new InvalidOperationException("Проверьте настройки сервиса отправки Telegram.");
        }

        var statusCode = response.StatusCode switch
        {
            HttpStatusCode.UnprocessableEntity or HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout
                => response.StatusCode,
            _ => HttpStatusCode.BadGateway
        };
        throw new HttpRequestException("Сервис отправки Telegram не подтвердил доставку.", null, statusCode);
    }
}
