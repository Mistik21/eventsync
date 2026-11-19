using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/maps")]
public class MapsController : ControllerBase
{
    [HttpGet("config")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetConfig([FromServices] IConfiguration configuration)
    {
        var apiKey = configuration["GoogleMaps:ApiKey"]?.Trim();
        return string.IsNullOrEmpty(apiKey) ? NoContent() : Ok(new { apiKey });
    }
}
