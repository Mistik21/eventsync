using System.Net;
using System.Net.Http.Headers;
using eventsynс.Application.Interfaces.Notifications;
using eventsynс.Domain.Repositories.Interfaces;

namespace eventsynс.Application.Services.Notifications;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly HttpClient client;
    private readonly IConfiguration configuration;
    private readonly IUserRepository userRepository;

    public TelegramNotificationService(HttpClient client, IConfiguration configuration, IUserRepository userRepository)
    {
        this.client = client;
        this.configuration = configuration;
        this.userRepository = userRepository;
    }

    public async Task SendMessageAsync(long telegramId, string message)
    {
        var relayUrl = configuration["Telegram:RelayUrl"]?.Trim();
        var relaySecret = configuration["Telegram:RelaySecret"]?.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Post, relayUrl)
        {
            Content = JsonContent.Create(new { chatId = telegramId, text = message })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", relaySecret);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpRequestException();
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
                errors.Add(new HttpRequestException());
            }
        }

        if (errors.Count > 0)
            throw new AggregateException(errors);
    }
}
