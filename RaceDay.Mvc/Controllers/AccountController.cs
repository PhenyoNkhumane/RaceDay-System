using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController(ApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model, string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var (user, cookie) = await api.SignInAsync<UserVm>("api/auth/login", new { model.Email, model.Password }, ct);
            if (user is null || string.IsNullOrWhiteSpace(cookie)) throw new ApiException("Sign in could not be completed.", System.Net.HttpStatusCode.Unauthorized);
            SaveUserSession(user, cookie);
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
            return user.Role == "Organiser" ? RedirectToAction("Index", "OrganiserDashboard") : RedirectToAction("Index", "ParticipantEvents");
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var (user, cookie) = await api.SignInAsync<UserVm>("api/auth/register", model, ct);
            if (user is null || string.IsNullOrWhiteSpace(cookie)) throw new ApiException("Registration could not be completed.", System.Net.HttpStatusCode.BadRequest);
            SaveUserSession(user, cookie);
            return user.Role == "Organiser" ? RedirectToAction("Index", "OrganiserDashboard") : RedirectToAction("Index", "ParticipantEvents");
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try { await api.PostNoContentAsync("api/auth/logout", new { }, ct); }
        catch (ApiException) { }
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private void SaveUserSession(UserVm user, string cookie)
    {
        HttpContext.Session.SetString("ApiCookie", cookie);
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("DisplayName", $"{user.FirstName} {user.LastName}");
    }
}
