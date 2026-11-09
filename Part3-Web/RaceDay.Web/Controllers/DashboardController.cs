using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class DashboardController : OrganiserControllerBase
{
    public DashboardController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<EventDto>>("api/events?mine=true");
        if (!result.Success)
            return Failed(result, "Index", "Home");

        return View(new DashboardViewModel { Events = result.Data ?? new List<EventDto>() });
    }
}
