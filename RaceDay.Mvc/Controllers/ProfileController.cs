using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
public class ProfileController(ApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Picture(CancellationToken ct)
    {
        try
        {
            var image = await api.GetImageAsync("api/users/me/profile-picture", ct);
            return image is null ? NotFound() : File(image.Value.Content, image.Value.ContentType);
        }
        catch (ApiException ex) { return StatusCode((int)ex.StatusCode); }
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var full = await api.GetAsync<ProfileVm>("api/users/me", ct);
            return View(full ?? new ProfileVm());
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; return View(new ProfileVm()); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProfileVm model, CancellationToken ct)
    {
        if (model.ProfilePicture is { Length: > 5_242_880 }) ModelState.AddModelError(nameof(model.ProfilePicture), "Image must be 5 MB or smaller.");
        if (!ModelState.IsValid) return View("Index", model);
        try
        {
            await api.PutAsync<UserVm>("api/users/me", new { model.FirstName, model.LastName, model.Email, model.IdNumberOrPassport, model.AsaPermanentLicenseNumber }, ct);
            if (model.ProfilePicture is { Length: > 0 })
            {
                var updated = await api.UploadAsync<UserVm>("api/users/me/profile-picture", model.ProfilePicture, ct);
                model.ProfilePictureUrl = updated?.ProfilePictureUrl;
            }
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("Index", model); }
    }
}
