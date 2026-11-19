using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/maps")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MapsController(IConfiguration configuration, IHttpClientFactory clients) : ControllerBase
{
    private readonly string? apiKey = configuration["TwoGis:ApiKey"]?.Trim();

    [HttpGet("config")]
    public IActionResult GetConfig() => string.IsNullOrEmpty(apiKey) ? NoContent() : Ok(new { apiKey });

    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken)
    {
        if (lat is not (>= -90 and <= 90) || lng is not (>= -180 and <= 180))
        {
            return BadRequest("Укажите широту и долготу в допустимых пределах.");
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var path = QueryHelpers.AddQueryString("3.0/items/geocode", new Dictionary<string, string?>
        {
            ["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture),
            ["lon"] = lng.Value.ToString(CultureInfo.InvariantCulture),
            ["fields"] = "items.adm_div,items.address",
            ["key"] = apiKey,
        });

        try
        {
            var response = await clients.CreateClient("TwoGisGeocoder")
                .GetFromJsonAsync<GeocoderResponse>(path, cancellationToken);
            if (response?.Meta?.Code == 404)
            {
                return NoContent();
            }
            if (response?.Meta?.Code != 200)
            {
                return StatusCode(StatusCodes.Status502BadGateway);
            }

            var item = response.Result?.Items?.FirstOrDefault();
            var address = string.IsNullOrWhiteSpace(item?.FullName) ? item?.AddressName : item.FullName;
            return string.IsNullOrWhiteSpace(address) ? NoContent() : Ok(new { address });
        }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private sealed record GeocoderResponse(GeocoderMeta? Meta, GeocoderResult? Result);
    private sealed record GeocoderMeta(int Code);
    private sealed record GeocoderResult(List<GeocoderItem>? Items);
    private sealed record GeocoderItem(
        [property: JsonPropertyName("full_name")] string? FullName,
        [property: JsonPropertyName("address_name")] string? AddressName);
}
