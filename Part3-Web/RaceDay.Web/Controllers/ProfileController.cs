using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Profile page for any logged-in user (Participant or Organiser).</summary>
public class ProfileController : Controller
{
    private const long MaxPictureBytes = 5 * 1024 * 1024;

    private readonly ApiClient _api;

    public ProfileController(ApiClient api) => _api = api;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) == null)
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = "/Profile" });

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<ProfileDto>("api/users/me");
        if (!result.Success || result.Data == null)
            return Failed(result);

        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        // Always reload the read-only parts (email, role, picture) from the API
        var current = await _api.GetAsync<ProfileDto>("api/users/me");
        if (!current.Success || current.Data == null)
            return Failed(current);

        model.Email = current.Data.Email;
        model.Role = current.Data.Role;
        model.ProfilePictureUrl = current.Data.ProfilePictureUrl;

        if (!ModelState.IsValid) return View(model);

        // The API replaces the picture URL on every update, so we send the existing one back
        var update = await _api.PutAsync<ProfileDto>("api/users/me", new
        {
            fullName = model.FullName.Trim(),
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            profilePictureUrl = current.Data.ProfilePictureUrl
        });

        if (update.Unauthorized) return Failed(update);
        if (!update.Success)
        {
            ModelState.AddModelError(string.Empty, update.Error ?? "We couldn't save your changes. Please try again.");
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.FullName, model.FullName.Trim());
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (picture.Length > MaxPictureBytes)
        {
            TempData["Error"] = "That image is too large. Please choose one under 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.PostFileAsync<ProfileDto>("api/users/me/picture", picture);
        if (result.Unauthorized) return Failed(result);
        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "We couldn't upload your picture. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        // Show the new picture in the navbar straight away
        if (!string.IsNullOrEmpty(result.Data?.ProfilePictureUrl))
            HttpContext.Session.SetString(SessionKeys.PictureUrl, result.Data.ProfilePictureUrl);

        TempData["Success"] = "Profile picture updated.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Failed<T>(ApiResult<T> result)
    {
        if (result.Unauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        TempData["Error"] = result.Error ?? "Something went wrong. Please try again.";
        return RedirectToAction("Index", "Home");
    }

    private static ProfileViewModel ToViewModel(ProfileDto dto) => new()
    {
        FullName = dto.FullName,
        PhoneNumber = dto.PhoneNumber,
        Email = dto.Email,
        Role = dto.Role,
        ProfilePictureUrl = dto.ProfilePictureUrl
    };
}
