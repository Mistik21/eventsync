using eventsynс.Application.Interfaces.Services;
using eventsynс.UI.Auth;
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
        if (!User.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var user = await userService.GetMyProfileAsync(currentUserId);
        var organizedCount = await userService.GetCountByOrganizerAsync(currentUserId);
        return Ok(new UserResponse(user.Id, user.Name, user.AttendedEventCount, organizedCount));
    }
}
