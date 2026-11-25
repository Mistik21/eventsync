using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eventsynс.UI.Controllers;

[AllowAnonymous]
public class PagesController : Controller
{
    [HttpGet("/")]
    [HttpGet("/createEvent")]
    [HttpGet("/joinEvent")]
    [HttpGet("/profile")]
    [HttpGet("/event/{id:guid}")]
    [HttpGet("/event/{id:guid}/activities")]
    [HttpGet("/event/{id:guid}/drives")]
    [HttpGet("/event/{id:guid}/items")]
    [HttpGet("/event/{id:guid}/budget")]
    [HttpGet("/event/{id:guid}/edit")]
    [HttpGet("/event/{id:guid}/participant")]
    [HttpGet("/html/index.html")]
    [HttpGet("/html/profile.html")]
    [HttpGet("/html/create-event.html")]
    [HttpGet("/html/join-event.html")]
    [HttpGet("/html/event.html")]
    [HttpGet("/html/activities.html")]
    [HttpGet("/html/drives.html")]
    [HttpGet("/html/items.html")]
    [HttpGet("/html/budget.html")]
    [HttpGet("/html/edit-event.html")]
    [HttpGet("/html/participants.html")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Frontend() => File("~/react/index.html", "text/html");
}
