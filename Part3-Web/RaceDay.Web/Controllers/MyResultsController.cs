using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class MyResultsController : ParticipantControllerBase
{
    public MyResultsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<MyResultDto>>("api/results/me");
        if (!result.Success)
            return Failed(result, "Index", "Home");

        return View(new MyResultsViewModel { Results = result.Data ?? new List<MyResultDto>() });
    }
}
