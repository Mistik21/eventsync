using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/cars")]
public class CarsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCars(Guid eventId) => Ok();

    [HttpGet("{carId:guid}")]
    public IActionResult GetCar(Guid eventId, Guid carId) => Ok();

    [HttpPost]
    public IActionResult CreateCar(Guid eventId) => Ok();

    [HttpPatch("{carId:guid}")]
    public IActionResult UpdateCar(Guid eventId, Guid carId) => Ok();

    [HttpDelete("{carId:guid}")]
    public IActionResult DeleteCar(Guid eventId, Guid carId) => NoContent();

    [HttpPost("{carId:guid}/join")]
    public IActionResult JoinCar(Guid eventId, Guid carId) => Ok();

    [HttpPost("{carId:guid}/leave")]
    public IActionResult LeaveCar(Guid eventId, Guid carId) => NoContent();
}