using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for the Participant pages: only logged-in Participants get in.</summary>
[RoleAuthorize(RoleNames.Participant)]
public abstract class ParticipantControllerBase : Controller
{
    protected ApiClient Api { get; }

    protected ParticipantControllerBase(ApiClient api) => Api = api;

    /// <summary>An expired session goes back to login; anything else shows the message on the given page.</summary>
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
}
