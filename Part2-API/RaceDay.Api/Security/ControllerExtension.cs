using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Security;

public static class ControllerExtensions
{
    /// <summary>The ID of the logged-in user, read from the session.</summary>
    public static int CurrentUserId(this ControllerBase controller) =>
        controller.HttpContext.Session.GetInt32(SessionKeys.UserId)
        ?? throw new InvalidOperationException("No user in session.");

    /// <summary>A 403 Forbidden response with a message.</summary>
    public static IActionResult ForbiddenResult(this ControllerBase controller, string message) =>
        controller.StatusCode(StatusCodes.Status403Forbidden, new { message });
    }