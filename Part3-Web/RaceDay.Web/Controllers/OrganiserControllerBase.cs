using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for every Organiser page: only logged-in Organisers get in.</summary>
[RoleAuthorize(RoleNames.Organiser)]
public abstract class OrganiserControllerBase : Controller
{
    protected ApiClient Api { get; }

    protected OrganiserControllerBase(ApiClient api) => Api = api;

    /// <summary>
    /// Handles a failed API call. An expired session goes back to login;
    /// anything else shows the message and returns to the given page.
    /// </summary>
    protected IActionResult Failed<T>(ApiResult<T> result, string action, string? controller = null, object? routeValues = null)
    {
        if (result.Unauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        TempData["Error"] = result.Error ?? "Something went wrong. Please try again.";
        return RedirectToAction(action, controller, routeValues);
    }

    /// <summary>Loads an event and makes sure the logged-in Organiser owns it.</summary>
    protected async Task<(EventDto? Event, IActionResult? Problem)> LoadOwnedEventAsync(int eventId)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (!result.Success || result.Data == null)
            return (null, Failed(result, "Index", "ManageEvents"));

        if (result.Data.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
        {
            TempData["Error"] = "You can only manage your own events.";
            return (null, RedirectToAction("Index", "ManageEvents"));
        }

        return (result.Data, null);
    }

    protected string? FirstModelError() =>
        ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
}
