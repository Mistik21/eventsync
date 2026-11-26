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
        var token = configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException();

        using var response = await client.PostAsJsonAsync(
            $"bot{token}/sendMessage",
            new { chat_id = telegramId, text = message });
        response.EnsureSuccessStatusCode();
    }
}
