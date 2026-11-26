using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Services;
using eventsynс.Domain;
using Microsoft.EntityFrameworkCore;
using eventsynс.Domain.Repositories;
using eventsynс.UI.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Authentication;
using eventsynс.Application.Interfaces.Notifications;


var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "UI", "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Add services to the container.

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICostService, CostService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ITelegramAuthValidator, TelegramAuthValidatorService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();

builder.Services.AddHttpClient<ITelegramNotificationService, TelegramNotificationService>(
        client => client.BaseAddress = new Uri("https://api.telegram.org/"))
    .RemoveAllLoggers();

builder.Services.AddAuthentication("EventSync")
    .AddPolicyScheme("EventSync", null, options =>
    {
        options.ForwardDefaultSelector = context => context.Request.Headers.ContainsKey(TelegramSessionHandler.HeaderName)
            ? TelegramSessionHandler.SchemeName
            : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, TelegramSessionHandler>(TelegramSessionHandler.SchemeName, null)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events = new SessionCookieEvents();
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();

}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = GetExceptionStatusCode,
    AllowStatusCode404Response = true
});

app.UseHttpsRedirection();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/react/assets"), assets => assets.UseResponseCompression());
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.Context.Request.Path.StartsWithSegments("/react/assets"))
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

var notifications = app.Services.GetRequiredService<ITelegramNotificationService>();
await notifications.SendMessageAsync(1664540300, "Тестовое уведомление EventSync");

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
