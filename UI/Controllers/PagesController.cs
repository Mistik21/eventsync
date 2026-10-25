using Microsoft.AspNetCore.Mvc;

namespace eventsync.UI.Controllers;

public class PagesController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() =>
        File("~/html/index.html", "text/html");

    [HttpGet("/createEvent")]
    public IActionResult CreateEvent() =>
        File("~/html/create-event.html", "text/html");

    [HttpGet("/profile")]
    public IActionResult Profile() =>
        File("~/html/profile.html", "text/html");

    [HttpGet("/event/{id:guid}")]
    public IActionResult Event(Guid id) =>
        File("~/html/event.html", "text/html");

    [HttpGet("/event/{id:guid}/activities")]
    public IActionResult Activities(Guid id) =>
        File("~/html/activities.html", "text/html");

    [HttpGet("/event/{id:guid}/drives")]
    public IActionResult Drives(Guid id) =>
        File("~/html/drives.html", "text/html");

    [HttpGet("/event/{id:guid}/items")]
    public IActionResult Items(Guid id) =>
        File("~/html/items.html", "text/html");

    [HttpGet("/event/{id:guid}/budget")]
    public IActionResult Budget(Guid id) =>
        File("~/html/budget.html", "text/html");

    [HttpGet("/event/{id:guid}/edit")]
    public IActionResult EditEvent(Guid id) =>
        File("~/html/edit-event.html", "text/html");

    [HttpGet("/event/{id:guid}/participant")]
    public IActionResult Participant(Guid id) =>
        File("~/html/participants.html", "text/html");
}