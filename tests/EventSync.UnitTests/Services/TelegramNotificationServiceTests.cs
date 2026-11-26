using System.Net;
using System.Text.Json;
using eventsynс.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.Services;

public class TelegramNotificationServiceTests
{
    [Fact]
    public async Task SendMessageAsync_UsesTelegramBaseAddressWithColonInBotToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Telegram:BotToken"] = "123456:fake_key" })
            .Build();
        using var client = new HttpClient(new TelegramHandler())
        {
            BaseAddress = new Uri("https://api.telegram.org/")
        };
        var service = new TelegramNotificationService(client, configuration);

        await service.SendMessageAsync(12345678901L, "Тестовое сообщение");
    }

    private sealed class TelegramHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.telegram.org/bot123456:fake_key/sendMessage", request.RequestUri?.AbsoluteUri);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.Equal(12345678901L, body.RootElement.GetProperty("chat_id").GetInt64());
            Assert.Equal("Тестовое сообщение", body.RootElement.GetProperty("text").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
