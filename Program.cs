using System.Security.Authentication;
using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Interfaces.Notifications;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Services;
using eventsynс.Domain;
using eventsynс.Domain.Repositories;
using eventsynс.UI.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;

const string authenticationScheme = "EventSync";
const string reactAssetsPath = "/react/assets";

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "UI", "wwwroot")
});
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICostService, CostService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITelegramAuthValidator, TelegramAuthValidatorService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddSingleton<IAmazonS3>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();

    return new AmazonS3Client(
        configuration["ObjectStorage:AccessKeyId"],
        configuration["ObjectStorage:SecretAccessKey"],
        new AmazonS3Config
        {
            ServiceURL = "https://s3.yandexcloud.net",
            AuthenticationRegion = "ru-central1",
            ForcePathStyle = true
        });
});
builder.Services.AddSingleton(services => new Lazy<IAmazonS3>(services.GetRequiredService<IAmazonS3>));
builder.Services.AddSingleton<IEventImageStorage, EventImageStorage>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ITelegramNotificationService, TelegramNotificationService>(
        client => client.Timeout = TimeSpan.FromSeconds(5))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .RemoveAllLoggers();

builder.Services.AddAuthentication(authenticationScheme)
    .AddPolicyScheme(authenticationScheme, null, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey(TelegramSessionHandler.HeaderName)
                ? TelegramSessionHandler.SchemeName
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, TelegramSessionHandler>(TelegramSessionHandler.SchemeName, null)
    .AddCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events = new SessionCookieEvents();
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = GetExceptionStatusCode,
    AllowStatusCode404Response = true
});

app.UseHttpsRedirection();

app.UseWhen(context => context.Request.Path.StartsWithSegments(reactAssetsPath),
    assets => assets.UseResponseCompression());
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.Context.Request.Path.StartsWithSegments(reactAssetsPath))
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();

static int GetExceptionStatusCode(Exception exception) => exception switch
{
    BadHttpRequestException badRequest => badRequest.StatusCode,
    ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
    InvalidCredentialException => StatusCodes.Status401Unauthorized,
    UnauthorizedAccessException => StatusCodes.Status403Forbidden,
    KeyNotFoundException => StatusCodes.Status404NotFound,
    DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
    InvalidOperationException invalidOperation when DomainConflict.IsConflict(invalidOperation) => StatusCodes.Status409Conflict,
    _ => StatusCodes.Status500InternalServerError
};
