using System.Globalization;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using eventsynс.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.Services;

public class TelegramAuthValidatorTests
{
    private const string TestBotToken = "unit-test-only-token";
    private const string ValidUserJson = """{"id":12345678901,"first_name":"Анна & Олег"}""";
    private readonly TelegramAuthValidatorService validator = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Telegram:BotToken"] = TestBotToken })
        .Build());

    [Fact]
    public void Constructor_RejectsMissingBotToken()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => new TelegramAuthValidatorService(configuration));
    }

    [Fact]
    public void Validate_ReturnsUserFromFreshSignedData()
    {
        var data = CreateSignedData(DateTimeOffset.UtcNow.AddMinutes(-1), ValidUserJson);

        var user = validator.Validate(data);

        Assert.Equal(12345678901L, user.TelegramId);
        Assert.Equal("Анна & Олег", user.Name);
    }

    [Fact]
    public void Validate_RejectsTamperedData()
    {
        var data = CreateSignedData(DateTimeOffset.UtcNow.AddMinutes(-1), ValidUserJson);

        Assert.Throws<InvalidCredentialException>(() => validator.Validate(data.Replace("12345678901", "12345678902")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("auth_date=123")]
    [InlineData("hash=letters-not-hex")]
    [InlineData("hash=00&hash=00")]
    public void Validate_RejectsMalformedQueryOrHash(string initData)
    {
        Assert.Throws<InvalidCredentialException>(() => validator.Validate(initData));
    }

    [Theory]
    [InlineData(-1500)]
    [InlineData(5)]
    public void Validate_RejectsExpiredAndFutureCredentials(int offsetMinutes)
    {
        var data = CreateSignedData(DateTimeOffset.UtcNow.AddMinutes(offsetMinutes), ValidUserJson);

        Assert.Throws<InvalidCredentialException>(() => validator.Validate(data));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("""{"id":0,"first_name":"Anna"}""")]
    [InlineData("""{"id":42,"first_name":" "}""")]
    public void Validate_RejectsInvalidUserEvenWithValidSignature(string? userJson)
    {
        var data = CreateSignedData(DateTimeOffset.UtcNow.AddMinutes(-1), userJson);

        Assert.Throws<InvalidCredentialException>(() => validator.Validate(data));
    }

    private static string CreateSignedData(DateTimeOffset authDate, string? userJson)
    {
        var timestamp = authDate.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        const string queryId = "unit-test";
        var data = "auth_date=" + timestamp + "\nquery_id=" + queryId;
        if (userJson is not null)
        {
            data += "\nuser=" + userJson;
        }

        var key = HMACSHA256.HashData("WebAppData"u8, Encoding.UTF8.GetBytes(TestBotToken));
        var signature = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data)));
        var query = $"query_id={queryId}&hash={signature}&auth_date={timestamp}";
        return userJson is null ? query : $"{query}&user={Uri.EscapeDataString(userJson)}";
    }
}
