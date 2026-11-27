using System.Net;
using System.Text.Json;
using eventsynс.Application.Services;
using EventSync.UnitTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.Services;

public class TelegramNotificationServiceTests
{
    private readonly IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:RelayUrl"] = "https://eventsync.example/send",
            ["Telegram:RelaySecret"] = "relay-secret"
        }).Build();

    [Theory]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task SendMessageAsync_UsesRelayAndRequiresDeliveryConfirmation(HttpStatusCode relayStatus)
    {
        using var client = new HttpClient(new RelayHandler(relayStatus, "Тестовое сообщение"));
        var service = new TelegramNotificationService(client, configuration, new InMemoryUserRepository());

        if (relayStatus == HttpStatusCode.NoContent)
        {
            await service.SendMessageAsync(12345678901L, "Тестовое сообщение");
            return;
        }

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendMessageAsync(12345678901L, "Тестовое сообщение"));
        Assert.Equal("Не доставлено", error.Message);
    }

    [Fact]
    public async Task SendPaymentRemindersAsync_SendsEachDebtInOneMessage()
    {
        var users = new InMemoryUserRepository
        {
            DebtsByTelegramId = new()
            {
                [12345678901L] = [("Поход", 500), ("Пикник", 150)]
            }
        };
        var handler = new RelayHandler(HttpStatusCode.NoContent,
            "У вас долг по событию Поход: 500 ₽\nУ вас долг по событию Пикник: 150 ₽");
        using var client = new HttpClient(handler);
        var service = new TelegramNotificationService(client, configuration, users);

        await service.SendPaymentRemindersAsync();

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SendPaymentRemindersAsync_SendsAtMostTenDebtsPerMessage()
    {
        var users = new InMemoryUserRepository
        {
            DebtsByTelegramId = new()
            {
                [12345678901L] = Enumerable.Range(1, 21).Select(number => ($"Событие {number}", number)).ToList()
            }
        };
        var handler = new RelayHandler(HttpStatusCode.NoContent,
            string.Join("\n", Enumerable.Range(1, 10).Select(number => $"У вас долг по событию Событие {number}: {number} ₽")),
            string.Join("\n", Enumerable.Range(11, 10).Select(number => $"У вас долг по событию Событие {number}: {number} ₽")),
            "У вас долг по событию Событие 21: 21 ₽");
        using var client = new HttpClient(handler);
        var service = new TelegramNotificationService(client, configuration, users);

        await service.SendPaymentRemindersAsync();

        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task SendPaymentRemindersAsync_ContinuesSendingAndReportsAllFailures()
    {
        var users = new InMemoryUserRepository
        {
            DebtsByTelegramId = new()
            {
                [101L] = [("Поход", 500)],
                [202L] = [("Пикник", 150)],
                [303L] = [("Экскурсия", 200)]
            }
        };
        var handler = new RelayHandler(
            (101L, "У вас долг по событию Поход: 500 ₽", HttpStatusCode.UnprocessableEntity),
            (202L, "У вас долг по событию Пикник: 150 ₽", HttpStatusCode.NoContent),
            (303L, "У вас долг по событию Экскурсия: 200 ₽", HttpStatusCode.TooManyRequests));
        using var client = new HttpClient(handler);
        var service = new TelegramNotificationService(client, configuration, users);

        var error = await Assert.ThrowsAsync<AggregateException>(service.SendPaymentRemindersAsync);

        Assert.Equal(3, handler.RequestCount);
        Assert.Equal(new[]
        {
            "Не удалось отправить напоминание пользователю Telegram 101.",
            "Не удалось отправить напоминание пользователю Telegram 303."
        }, error.InnerExceptions.Select(failure => failure.Message));
    }

    private sealed class RelayHandler(params (long ChatId, string Text, HttpStatusCode StatusCode)[] expectedRequests)
        : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public RelayHandler(HttpStatusCode statusCode, params string[] expectedMessages)
            : this(expectedMessages.Select(message => (12345678901L, message, statusCode)).ToArray())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://eventsync.example/send", request.RequestUri?.AbsoluteUri);
            Assert.Equal("Bearer relay-secret", request.Headers.Authorization?.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var expected = expectedRequests[RequestCount];
            Assert.Equal(expected.ChatId, body.RootElement.GetProperty("chatId").GetInt64());
            Assert.Equal(expected.Text, body.RootElement.GetProperty("text").GetString());
            RequestCount++;

            return new HttpResponseMessage(expected.StatusCode);
        }
    }
}
