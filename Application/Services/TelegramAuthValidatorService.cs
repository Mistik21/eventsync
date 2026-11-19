using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Models.Auth;
using Microsoft.AspNetCore.WebUtilities;

namespace eventsynс.Application.Services;

public class TelegramAuthValidatorService : ITelegramAuthValidator
{
    private readonly string botToken;

    public TelegramAuthValidatorService(IConfiguration configuration)
    {
        botToken = configuration["Telegram:BotToken"]
                   ?? throw new InvalidOperationException("Не настроен Telegram:BotToken.");

        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException("Не настроен Telegram:BotToken.");
    }

    public Task<TelegramUserModel> ValidateAsync(string initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
            throw new UnauthorizedAccessException("Данные Telegram отсутствуют.");

        var fields = QueryHelpers.ParseQuery(initData);
        if (fields.Any(field => field.Value.Count != 1) ||
            !fields.TryGetValue("hash", out var hashValue))
            throw new UnauthorizedAccessException("Некорректные данные Telegram.");

        byte[] receivedHash;
        try
        {
            receivedHash = Convert.FromHexString(hashValue.ToString());
        }
        catch (FormatException)
        {
            throw new UnauthorizedAccessException("Некорректная подпись Telegram.");
        }

        var dataCheckString = string.Join("\n", fields
            .Where(field => field.Key != "hash")
            .OrderBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => $"{field.Key}={field.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var expectedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicOperations.FixedTimeEquals(expectedHash, receivedHash))
            throw new UnauthorizedAccessException("Неверная подпись Telegram.");

        if (!fields.TryGetValue("auth_date", out var authDateValue) ||
            !long.TryParse(authDateValue.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var authDate))
            throw new UnauthorizedAccessException("Некорректное время авторизации Telegram.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (authDate > now || authDate < now - 86400)
            throw new UnauthorizedAccessException("Данные авторизации Telegram устарели или ещё не действуют.");

        if (!fields.TryGetValue("user", out var userValue))
            throw new UnauthorizedAccessException("Данные пользователя Telegram отсутствуют.");

        try
        {
            using var document = JsonDocument.Parse(userValue.ToString());
            var user = document.RootElement;
            if (user.ValueKind != JsonValueKind.Object ||
                !user.TryGetProperty("id", out var idElement) ||
                idElement.ValueKind != JsonValueKind.Number || !idElement.TryGetInt64(out var id) || id <= 0 ||
                !user.TryGetProperty("first_name", out var nameElement) ||
                nameElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameElement.GetString()))
                throw new UnauthorizedAccessException("Некорректные данные пользователя Telegram.");

            return Task.FromResult(new TelegramUserModel(id, nameElement.GetString()!));
        }
        catch (JsonException)
        {
            throw new UnauthorizedAccessException("Некорректные данные пользователя Telegram.");
        }
    }
}