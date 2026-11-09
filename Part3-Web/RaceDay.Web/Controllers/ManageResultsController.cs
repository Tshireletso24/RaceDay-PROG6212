using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ManageResultsController : OrganiserControllerBase
{
    public ManageResultsController(ApiClient api) : base(api) { }

    // id = the event's ID
    public async Task<IActionResult> Index(int id)
    {
        var (ev, problem) = await LoadOwnedEventAsync(id);
        if (problem != null) return problem;

        var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        if (!enrolments.Success)
            return Failed(enrolments, "Index", "ManageEvents");

        return View(new ResultsPageViewModel
        {
            Event = ev!,
            // Cancelled entries never finish, so they are left out
            Enrolments = (enrolments.Data ?? new List<EnrolmentDto>())
                .Where(e => e.Status != "Cancelled")
                .OrderBy(e => e.HasResult ? e.FinishPosition : int.MaxValue)
                .ThenBy(e => e.ParticipantName)
                .ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Capture(ResultInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError() ?? "Please check the finish time and position.";
            return RedirectToAction(nameof(Index), new { id = model.EventId });
        }

        var result = await Api.PostAsync<ResultDto>("api/results", new
        {
            enrolmentId = model.EnrolmentId,
            finishTime = model.FinishTime,
            finishPosition = model.FinishPosition
        });
        if (result.Unauthorized) return Failed(result, nameof(Index), null, new { id = model.EventId });

        if (result.Success) TempData["Success"] = "Result published. The participant can now see it.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { id = model.EventId });
    }
}
