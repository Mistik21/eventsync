using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMyProfile() => Ok();
}