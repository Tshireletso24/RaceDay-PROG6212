using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (result, cookie) = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data == null || cookie == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't log you in. Please try again.");
            return View(model);
        }

        // Remember who this is (and the API's session cookie) in the MVC session
        var user = result.Data;
        var session = HttpContext.Session;
        session.Clear();
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FullName, user.FullName);
        session.SetString(SessionKeys.ApiCookie, cookie);
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            session.SetString(SessionKeys.PictureUrl, user.ProfilePictureUrl);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Organiser" && string.IsNullOrWhiteSpace(model.OrganiserCode))
            ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers need an invite code.");

        if (!ModelState.IsValid) return View(model);

        var body = new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber,
            organiserCode = model.Role == "Organiser" ? model.OrganiserCode : null
        };

        var result = await _api.PostAsync<UserDto>("api/auth/register", body);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't create your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Your account is ready. Log in to get started.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Logout ----------

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Ask the API to end its session too (best effort), then clear ours
        await _api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();

        TempData["Success"] = "You've been logged out. See you at the next start line!";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
