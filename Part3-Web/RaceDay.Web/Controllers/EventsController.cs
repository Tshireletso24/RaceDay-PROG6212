using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public event details, plus the Enter Event action for Participants.</summary>
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetAsync<BrowseEventDto>($"api/events/{id}");
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.NotFound ? "We couldn't find that event." : (result.Error ?? "We couldn't load that event.");
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel
        {
            Event = result.Data,
            Role = HttpContext.Session.GetString(SessionKeys.Role)
        };

        // A Participant may already be entered (the API allows one entry per event)
        if (model.IsParticipant)
        {
            var mine = await _api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");
            if (mine.Success)
                model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Enter(int eventId, int categoryId)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) == null)
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Events/Details/{eventId}" });

        if (HttpContext.Session.GetString(SessionKeys.Role) != RoleNames.Participant)
            return RedirectToAction("AccessDenied", "Account");

        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category before entering.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.PostAsync<MyEnrolmentDto>("api/enrolments", new { eventId, categoryId });
        if (result.Unauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "We couldn't enter you in this event. Please try again.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You're in! Your entry is Pending until the organiser confirms it.";
        return RedirectToAction("Index", "MyEvents");
    }
}
