using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ManageEventsController : OrganiserControllerBase
{
    public ManageEventsController(ApiClient api) : base(api) { }

    // ---------- List ----------

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<EventDto>>("api/events?mine=true");
        if (!result.Success)
            return Failed(result, "Index", "Home");

        return View(result.Data ?? new List<EventDto>());
    }

    // ---------- Create ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadEventTypesAsync();
        return View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(6) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        await LoadEventTypesAsync();
        if (!ModelState.IsValid) return View("Form", model);

        var result = await Api.PostAsync<EventDto>("api/events", ToRequest(model));
        if (result.Unauthorized) return Failed(result, nameof(Index));
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't create the event. Please try again.");
            return View("Form", model);
        }

        // The banner goes to the API, which stores it in Azure Blob Storage
        var uploadError = await UploadBannerIfAnyAsync(result.Data.EventId, model.BannerFile);
        TempData["Success"] = uploadError == null
            ? "Event created."
            : $"Event created, but the banner couldn't be uploaded: {uploadError}";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, problem) = await LoadOwnedEventAsync(id);
        if (problem != null) return problem;

        await LoadEventTypesAsync();
        return View("Form", new EventFormViewModel
        {
            EventId = ev!.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventTypeId = ev.EventTypeId,
            ExistingImageUrl = ev.ImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EventFormViewModel model)
    {
        if (model.EventId == null) return RedirectToAction(nameof(Index));

        await LoadEventTypesAsync();
        if (!ModelState.IsValid) return View("Form", model);

        var result = await Api.PutAsync<EventDto>($"api/events/{model.EventId}", ToRequest(model));
        if (result.Unauthorized) return Failed(result, nameof(Index));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save your changes. Please try again.");
            return View("Form", model);
        }

        var uploadError = await UploadBannerIfAnyAsync(model.EventId.Value, model.BannerFile);
        TempData["Success"] = uploadError == null
            ? "Event updated."
            : $"Event updated, but the banner couldn't be uploaded: {uploadError}";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Delete ----------

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        if (result.Unauthorized) return Failed(result, nameof(Index));

        if (result.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ---------- Helpers ----------

    private async Task LoadEventTypesAsync()
    {
        var types = await Api.GetAsync<List<EventTypeDto>>("api/eventtypes");
        ViewBag.EventTypes = new SelectList(types.Data ?? new List<EventTypeDto>(), "EventTypeId", "TypeName");
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        name = m.Name,
        description = m.Description,
        eventDate = m.EventDate,
        location = m.Location,
        distanceKm = m.DistanceKm,
        eventTypeId = m.EventTypeId,
        // Keep the current banner; a newly uploaded file replaces it afterwards
        imageUrl = m.ExistingImageUrl
    };

    private async Task<string?> UploadBannerIfAnyAsync(int eventId, IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var upload = await Api.PostFileAsync<EventDto>($"api/events/{eventId}/banner", file);
        return upload.Success ? null : upload.Error;
    }
}
