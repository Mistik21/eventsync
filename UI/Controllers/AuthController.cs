using eventsynс.UI.DTOs.Requests.Auth;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

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