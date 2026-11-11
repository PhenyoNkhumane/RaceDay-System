using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Route("Events")]
[SessionAuthorize("Participant")]
public class ParticipantEventsController(ApiClient api) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? type, DateOnly? from, decimal? maxDistance, CancellationToken ct)
    {
        try
        {
            var events = await api.GetAsync<List<EventVm>>("api/events", ct) ?? [];
            var model = new EventListVm { Type = type, From = from, MaxDistance = maxDistance };
            model.Events = events.Where(e => e.Date >= (from ?? DateOnly.FromDateTime(DateTime.Today)))
                .Where(e => string.IsNullOrWhiteSpace(type) || e.EventType == type)
                .Where(e => maxDistance is null || e.DistanceKm <= maxDistance)
                .OrderBy(e => e.Date).ToList();
            return View(model);
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new EventListVm()); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        try
        {
            var ev = await api.GetAsync<EventVm>($"api/events/{id}", ct);
            var cats = await api.GetAsync<List<CategoryVm>>($"api/events/{id}/categories", ct) ?? [];
            return View(new EventDetailVm { Event = ev ?? new EventVm(), Categories = cats });
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpGet("{id:int}/banner")]
    public async Task<IActionResult> Banner(int id, CancellationToken ct)
    {
        try
        {
            var image = await api.GetImageAsync($"api/events/{id}/banner", ct);

            if (image is null)
                return NotFound();

            return File(image.Value.Content, image.Value.ContentType);
        }
        catch (ApiException ex)
        {
            return StatusCode((int)ex.StatusCode);
        }
    }

    [HttpPost("{id:int}/enrol"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, EnrolFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Choose a category before entering."; return RedirectToAction(nameof(Details), new { id }); }
        try
        {
            await api.PostWithResponseAsync<RegistrationVm>($"api/events/{id}/registrations", model, ct);
            TempData["Success"] = "Your enrolment has been recorded.";
            return RedirectToAction("Index", "ParticipantEnrolments");
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Details), new { id }); }
    }
}

[SessionAuthorize("Participant")]
public class ParticipantEnrolmentsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await api.GetAsync<List<RegistrationVm>>("api/registrations/me", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<RegistrationVm>()); }
    }
}

[SessionAuthorize("Participant")]
public class ParticipantResultsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await api.GetAsync<List<ResultVm>>("api/results/me", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<ResultVm>()); }
    }
}
