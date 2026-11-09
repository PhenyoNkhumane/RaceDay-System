using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Organiser")]
public class OrganiserDashboardController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await api.GetAsync<List<EventSummaryVm>>("api/events/dashboard", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<EventSummaryVm>()); }
    }
}

[SessionAuthorize("Organiser")]
public class OrganiserEventsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await api.GetAsync<List<EventVm>>("api/events/mine", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<EventVm>()); }
    }
    [HttpGet] public IActionResult Create() => View("Form", new EventFormVm { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) });
    [HttpGet] public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        try
        {
            var ev = (await api.GetAsync<List<EventVm>>("api/events/mine", ct) ?? []).FirstOrDefault(e => e.Id == id);
            if (ev is null) return NotFound();
            return View("Form", new EventFormVm { Id = ev.Id, Title = ev.Title, Description = ev.Description, Date = ev.Date, VenueName = ev.VenueName, City = ev.City, Province = ev.Province, DistanceKm = ev.DistanceKm, EventType = ev.EventType, Status = ev.Status, BannerUrl = ev.BannerUrl });
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EventFormVm model, CancellationToken ct)
    {
        if (model.Banner is { Length: > 5_242_880 }) ModelState.AddModelError(nameof(model.Banner), "Image must be 5 MB or smaller.");
        if (!ModelState.IsValid) return View("Form", model);
        var payload = new { model.Title, model.Description, model.Date, model.VenueName, model.City, model.Province, model.DistanceKm, model.EventType, model.Status };
        EventVm? ev = null;
        try
        {
            if (model.Id == 0) ev = await api.PostAsync<EventVm>("api/events", payload, ct);
            else ev = await api.PutAsync<EventVm>($"api/events/{model.Id}", payload, ct);
            if (ev is not null && model.Banner is { Length: > 0 }) await api.UploadAsync<EventVm>($"api/events/{ev.Id}/banner", model.Banner, ct);
            TempData["Success"] = "Event saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            if (model.Id == 0 && ev is not null)
            {
                TempData["Error"] = $"The event was saved, but the banner upload failed: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = ev.Id });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await api.DeleteAsync($"api/events/{id}", ct); TempData["Success"] = "Event deleted."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

[SessionAuthorize("Organiser")]
public class OrganiserCategoriesController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        try { ViewBag.EventId = eventId; ViewBag.Disciplines = await api.GetAsync<List<DisciplineVm>>("api/disciplines", ct) ?? []; return View(await api.GetAsync<List<CategoryVm>>($"api/events/{eventId}/categories", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<CategoryVm>()); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Check the category details."; return RedirectToAction(nameof(Index), new { eventId = model.EventId }); }
        var payload = new { model.DisciplineId, model.Name, model.CategoryType, model.DistanceKm, model.MinAge, model.MaxAge, model.EntryFeeZar, model.MaxSlots, model.StartTime };
        try { await api.PostWithResponseAsync<CategoryVm>($"api/events/{model.EventId}/categories", payload, ct); TempData["Success"] = "Category added."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CategoryFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Check the category details."; return RedirectToAction(nameof(Index), new { eventId = model.EventId }); }
        var payload = new { model.Name, model.CategoryType, model.DistanceKm, model.MinAge, model.MaxAge, model.EntryFeeZar, model.MaxSlots, model.StartTime };
        try { await api.PutAsync<CategoryVm>($"api/categories/{model.Id}", payload, ct); TempData["Success"] = "Category updated."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId, CancellationToken ct)
    {
        try { await api.DeleteAsync($"api/categories/{id}", ct); TempData["Success"] = "Category deleted."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { eventId });
    }
}

[SessionAuthorize("Organiser")]
public class OrganiserEnrolmentsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        try { ViewBag.EventId = eventId; return View(await api.GetAsync<List<RegistrationVm>>($"api/events/{eventId}/registrations", ct) ?? []); }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<RegistrationVm>()); }
    }
}

[SessionAuthorize("Organiser")]
public class OrganiserResultsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        try
        {
            ViewBag.EventId = eventId;
            ViewBag.Registrations = await api.GetAsync<List<RegistrationVm>>($"api/events/{eventId}/registrations", ct) ?? [];
            return View(await api.GetAsync<List<ResultVm>>($"api/events/{eventId}/results/manage", ct) ?? []);
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new List<ResultVm>()); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, ResultFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Check the result details."; return RedirectToAction(nameof(Index), new { eventId }); }
        var payload = new { model.RegistrationId, model.GunTime, model.NetTime, model.OverallPosition, model.CategoryPosition, model.Status };
        try { await api.PostWithResponseAsync<ResultVm>($"api/events/{eventId}/results", payload, ct); TempData["Success"] = "Result recorded and published."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
