using System.Security.Claims;
using eventsynс.Application.Services;
using eventsynс.UI.Controllers;
using eventsynс.UI.DTOs.Responses;
using EventSync.UnitTests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EventSync.UnitTests.UI;

public class UsersControllerTests
{
    [Fact]
    public async Task GetMyProfile_ReturnsProfileAndSeparateEventCounts()
    {
        var user = TestData.CreateUser(1, "Anna");
        var other = TestData.CreateUser(2);
        var events = new InMemoryEventRepository(
            TestData.CreateEvent(user), TestData.CreateEvent(other, user), TestData.CreateEvent(other));
        var controller = new UsersController(new UserService(new InMemoryUserRepository(user), events))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
                        authenticationType: "test"))
                }
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.GetMyProfile());

        Assert.Equal(new UserResponse(user.Id, user.Name, 2, 1), Assert.IsType<UserResponse>(result.Value));
    }
}
