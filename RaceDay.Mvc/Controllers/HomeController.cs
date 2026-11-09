using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => HttpContext.Session.GetString("Role") switch
    {
        "Organiser" => RedirectToAction("Index", "OrganiserDashboard"),
        "Participant" => RedirectToAction("Index", "ParticipantEvents"),
        _ => RedirectToAction("Index", "ParticipantEvents")
    };

    public IActionResult Error() => View();
}

[Route("Errors")]
public class ErrorsController : Controller
{
    [Route("Error")]
    public IActionResult Error() => View("~/Views/Home/Error.cshtml");
}
