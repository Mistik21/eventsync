using System.Net;

namespace eventsynс.Application.Services;

public sealed class TelegramRelayException(HttpStatusCode statusCode, HttpStatusCode relayStatusCode,
    string? telegramResult) : HttpRequestException("Сервис отправки Telegram вернул ошибку.", null, statusCode)
{
    public HttpStatusCode RelayStatusCode { get; } = relayStatusCode;
    public string? TelegramResult { get; } = telegramResult;
}
