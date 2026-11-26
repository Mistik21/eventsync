using System.Net;
using System.Text.Json;
using eventsynс.Application.Interfaces.Notifications;
using eventsynс.Application.Services;
using eventsynс.UI.Controllers;
using EventSync.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventSync.UnitTests.UI;

public class TelegramConnectionTests
{
    [Fact]
    public async Task CheckConnection_PreservesHttpResponseAndConnectionTimeout()
    {
        var users = new UserService(new InMemoryUserRepository(), new InMemoryEventRepository());
        var controller = new TelegramController(users, new UnusedNotifications(), new ProbeClientFactory(),
            NullLogger<TelegramController>.Instance);

        var result = Assert.IsType<OkObjectResult>(await controller.CheckConnection(CancellationToken.None));
        var probes = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        Assert.Equal("api.telegram.org", probes[0].GetProperty("host").GetString());
        Assert.Equal(302, probes[0].GetProperty("httpStatus").GetInt32());
        Assert.Equal("example.com", probes[1].GetProperty("host").GetString());
        Assert.Equal("Таймаут", probes[1].GetProperty("error").GetString());
    }

    private sealed class ProbeClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new ProbeHandler());
    }

    private sealed class ProbeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.RequestUri?.Host == "api.telegram.org"
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.Found))
                : Task.FromException<HttpResponseMessage>(new TaskCanceledException());
    }

    private sealed class UnusedNotifications : ITelegramNotificationService
    {
        public Task SendMessageAsync(long telegramId, string message) =>
            throw new InvalidOperationException("Connection diagnostics must not send a notification.");
    }
}
