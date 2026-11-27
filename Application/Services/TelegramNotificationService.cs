using System.Net;
using System.Net.Http.Headers;
using eventsynс.Application.Interfaces.Notifications;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class TelegramNotificationService(HttpClient client, IConfiguration configuration,IUserRepository userRepository) : ITelegramNotificationService
{
    public async Task SendMessageAsync(long telegramId, string message)
    {
        var relayUrl = configuration["Telegram:RelayUrl"]?.Trim();
        if (!Uri.TryCreate(relayUrl, UriKind.Absolute, out var relayAddress) || relayAddress.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Для отправки в Telegram укажите HTTPS-адрес relay.");

        var relaySecret = configuration["Telegram:RelaySecret"]?.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Post, relayAddress)
        {
            Content = JsonContent.Create(new { chatId = telegramId, text = message })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", relaySecret);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpRequestException("Не доставлено");
    }

    public async Task SendPaymentRemindersAsync()
    {
        var debtors = await userRepository.ListDebtorsAsync();
        var errors = new List<Exception>();
        foreach (var debtor in debtors)
        {
            var lines = debtor.Value.Select(debt =>
                $"У вас долг по событию {debt.EventTitle}: {debt.Amount} ₽");
            try
            {
                foreach (var batch in lines.Chunk(10))
                {
                    await SendMessageAsync(debtor.Key, string.Join("\n", batch));
                }
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
            {
                errors.Add(new HttpRequestException(
                    $"Не удалось отправить напоминание пользователю Telegram {debtor.Key}."));
            }
        }

        if (errors.Count > 0)
            throw new AggregateException("При отправке напоминаний в Telegram произошли ошибки.", errors);
    }
}
