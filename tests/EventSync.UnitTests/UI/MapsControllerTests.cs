using System.Net;
using System.Text;
using System.Text.Json;
using eventsynс.UI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.UI;

public class MapsControllerTests
{
    [Theory]
    [InlineData(null, 60.0)]
    [InlineData(56.0, null)]
    [InlineData(90.01, 60.0)]
    [InlineData(56.0, -180.01)]
    public async Task Reverse_RejectsInvalidCoordinatesBeforeCallingProvider(double? latitude, double? longitude)
    {
        var controller = CreateController("test-key");

        var result = await controller.Reverse(latitude, longitude, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MissingKey_DisablesMapConfigurationAndReverseLookup()
    {
        var controller = CreateController(" ");

        Assert.IsType<NoContentResult>(controller.GetConfig());
        var result = Assert.IsType<StatusCodeResult>(await controller.Reverse(56, 60, CancellationToken.None));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    [Theory]
    [InlineData("Full address", "Short address", "Full address")]
    [InlineData(" ", "Short address", "Short address")]
    public async Task Reverse_MapsProviderAddressAndSendsCoordinates(string fullName, string addressName, string expected)
    {
        var json = JsonSerializer.Serialize(new
        {
            meta = new { code = 200 },
            result = new { items = new[] { new { full_name = fullName, address_name = addressName } } }
        });
        using var client = CreateClient(request =>
        {
            Assert.Equal("/3.0/items/geocode", request.RequestUri!.AbsolutePath);
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal("56.84", query["lat"].ToString());
            Assert.Equal("60.61", query["lon"].ToString());
            Assert.Equal("test-key", query["key"].ToString());
            return JsonResponse(json);
        });

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(" test-key ", client).Reverse(56.84, 60.61, CancellationToken.None));

        Assert.Equal(expected, JsonSerializer.SerializeToElement(result.Value).GetProperty("address").GetString());
    }

    [Theory]
    [InlineData("""{"meta":{"code":404}}""")]
    [InlineData("""{"meta":{"code":200},"result":{"items":[]}}""")]
    public async Task Reverse_ReturnsNoContentWhenAddressIsUnavailable(string json)
    {
        using var client = CreateClient(_ => JsonResponse(json));

        var result = await CreateController("test-key", client).Reverse(56, 60, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Theory]
    [InlineData("""{"meta":{"code":500}}""")]
    [InlineData("not-json")]
    public async Task Reverse_ReportsBadGatewayForProviderErrors(string json)
    {
        using var client = CreateClient(_ => JsonResponse(json));

        var result = Assert.IsType<StatusCodeResult>(
            await CreateController("test-key", client).Reverse(56, 60, CancellationToken.None));

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
    }

    private static MapsController CreateController(string? apiKey, HttpClient? client = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TwoGis:ApiKey"] = apiKey })
            .Build();
        return new MapsController(configuration, new StubClientFactory(client));
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new StubHandler(response)) { BaseAddress = new Uri("https://maps.example.test") };

    private static HttpResponseMessage JsonResponse(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class StubClientFactory(HttpClient? client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("TwoGisGeocoder", name);
            return client ?? throw new InvalidOperationException("The provider should not be called.");
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}
