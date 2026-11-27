using eventsynс.Application.Interfaces.Auth;
using eventsynс.Application.Interfaces.Users;
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
    private readonly IUserService userService;

    public AuthController(IAuthService authService,IUserService userService)
    {
        this.authService = authService;
        this.userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramLogin([FromBody] TelegramLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InitData))
        {
            return BadRequest();
        }

        var user = await authService.AuthenticateTelegramAsync(request.InitData);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            CookieAuthenticationDefaults.AuthenticationScheme);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        var attendedCount = await userService.GetCountByParticipantAsync(user.Id);
        return Ok(new UserResponse(user.Id, user.Name, attendedCount));
    }
}
