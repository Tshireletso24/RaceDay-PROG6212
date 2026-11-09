using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    // Public: anyone can browse upcoming events
    public async Task<IActionResult> Index(int? eventTypeId, string? search, bool includePast = false)
    {
        var model = new HomeViewModel
        {
            EventTypeId = eventTypeId,
            Search = search,
            IncludePast = includePast
        };

        var query = new List<string>();
        if (eventTypeId.HasValue) query.Add($"eventTypeId={eventTypeId.Value}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (includePast) query.Add("includePast=true");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var events = await _api.GetAsync<List<BrowseEventDto>>(url);
        if (events.Success) model.Events = events.Data ?? new List<BrowseEventDto>();
        else model.LoadError = events.Error;

        var types = await _api.GetAsync<List<EventTypeDto>>("api/eventtypes");
        if (types.Success) model.EventTypes = types.Data ?? new List<EventTypeDto>();

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
