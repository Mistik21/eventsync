using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Services;
using Microsoft.EntityFrameworkCore;
using eventsynс.Domain.Repositories;
using eventsynс.UI.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "UI", "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});
builder.WebHost.UseSetting("DetailedErrors", "true");

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Add services to the container.

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ITelegramAuthValidator, TelegramAuthValidatorService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();

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
builder.Services.AddHttpClient("TwoGisGeocoder", client =>
{
    client.BaseAddress = new Uri("https://catalog.api.2gis.com/");
    client.Timeout = TimeSpan.FromSeconds(2.5);
}).RemoveAllLoggers();

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

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
