using Microsoft.AspNetCore.Mvc;
using UI.DTOs.Requests.Auth;

namespace UI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("telegram")]
    public IActionResult TelegramLogin(
        [FromBody] TelegramLoginRequest request)
    {
        return Ok();
    }
}