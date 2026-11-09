using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ManageEnrolmentsController : OrganiserControllerBase
{
    public ManageEnrolmentsController(ApiClient api) : base(api) { }

    // id = the event's ID
    public async Task<IActionResult> Index(int id)
    {
        var (ev, problem) = await LoadOwnedEventAsync(id);
        if (problem != null) return problem;

        var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        if (!enrolments.Success)
            return Failed(enrolments, "Index", "ManageEvents");

        return View(new EnrolmentsPageViewModel
        {
            Event = ev!,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>()
        });
    }

    // id = the enrolment's ID
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string status)
    {
        var result = await Api.PutAsync<EnrolmentDto>($"api/enrolments/{id}/status", new { status });
        if (result.Unauthorized) return Failed(result, nameof(Index), null, new { id = eventId });

        if (result.Success) TempData["Success"] = $"Enrolment marked as {status}.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { id = eventId });
    }
}
