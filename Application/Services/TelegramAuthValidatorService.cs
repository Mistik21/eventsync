using System.Globalization;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Models.Auth;
using Microsoft.AspNetCore.WebUtilities;

namespace eventsynс.Application.Services;

public class TelegramAuthValidatorService : ITelegramAuthValidator
{
    private readonly byte[] secretKey;

    public TelegramAuthValidatorService(IConfiguration configuration)
    {
        var botToken = configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException("Не настроен Telegram:BotToken.");

        secretKey = HMACSHA256.HashData("WebAppData"u8, Encoding.UTF8.GetBytes(botToken));
    }

    public TelegramUserModel Validate(string initData)
    {
        var fields = QueryHelpers.ParseQuery(initData);
        if (!fields.TryGetValue("hash", out var hashValue) ||
            fields.Any(field => field.Value.Count != 1))
            throw new InvalidCredentialException("Некорректные данные Telegram.");

        byte[] receivedHash;
        try
        {
            receivedHash = Convert.FromHexString(hashValue.ToString());
        }
        catch (FormatException)
        {
            throw new InvalidCredentialException("Некорректная подпись Telegram.");
        }

        var dataCheckString = string.Join("\n", fields
            .Where(field => field.Key != "hash")
            .OrderBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => $"{field.Key}={field.Value}"));

        var expectedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicOperations.FixedTimeEquals(expectedHash, receivedHash))
            throw new InvalidCredentialException("Неверная подпись Telegram.");

        if (!fields.TryGetValue("auth_date", out var authDateValue) ||
            !long.TryParse(authDateValue.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var authDate))
            throw new InvalidCredentialException("Некорректное время авторизации Telegram.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (authDate > now || authDate < now - 86400)
            throw new InvalidCredentialException("Данные авторизации Telegram устарели или ещё не действуют.");

        if (!fields.TryGetValue("user", out var userValue))
            throw new InvalidCredentialException("Данные пользователя Telegram отсутствуют.");

        try
        {
            var user = JsonSerializer.Deserialize<TelegramUserPayload>(userValue.ToString());
            if (user is null || user.Id <= 0 || string.IsNullOrWhiteSpace(user.FirstName))
                throw new InvalidCredentialException("Некорректные данные пользователя Telegram.");

            return new TelegramUserModel(user.Id, user.FirstName);
        }
        catch (JsonException exception)
        {
            throw new InvalidCredentialException("Некорректные данные пользователя Telegram.", exception);
        }
    }

    private sealed record TelegramUserPayload(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("first_name")] string? FirstName);
}
