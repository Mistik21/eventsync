using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.DTOs.Requests.Auth;
using eventsynс.UI.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace eventsynс.UI.Controllers;

[ApiController] 
[Route("api/auth")] 
public class AuthController : ControllerBase 
{ 
    private readonly IAuthService authService; 
 
    public AuthController(IAuthService authService) 
    { 
        this.authService = authService; 
    } 
    
    [Authorize]
    [HttpGet("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetSession() => NoContent();

    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramLogin([FromBody] TelegramLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InitData))
        {
            return BadRequest("Не переданы данные Telegram.");
        }

        try
        {
            var user = await authService.AuthenticateTelegramAsync(request.InitData);

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Ok(new UserResponse(
                user.Id,
                user.Name,
                user.AttendedEventCount
                ));
        }
        catch (UnauthorizedAccessException) 
        { 
            return Unauthorized("Не удалось проверить данные Telegram."); 
        }
    }
}
