using System.Security.Claims;
using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var user = await userService.GetMyProfileAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserResponse(user.Id, user.Name, user.AttendedEventCount,
            await userService.GetCountByOrganizerAsync(userId)));
    }
}