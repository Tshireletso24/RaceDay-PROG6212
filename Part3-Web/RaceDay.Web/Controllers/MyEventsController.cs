using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class MyEventsController : ParticipantControllerBase
{
    public MyEventsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");
        if (!result.Success)
            return Failed(result, "Index", "Home");

        var all = result.Data ?? new List<MyEnrolmentDto>();
        var today = DateTime.Today;

        return View(new MyEventsViewModel
        {
            Upcoming = all.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            Past = all.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList()
        });
    }
}
