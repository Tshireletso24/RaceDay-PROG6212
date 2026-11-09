using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ManageCategoriesController : OrganiserControllerBase
{
    public ManageCategoriesController(ApiClient api) : base(api) { }

    // id = the event's ID
    public async Task<IActionResult> Index(int id)
    {
        var (ev, problem) = await LoadOwnedEventAsync(id);
        if (problem != null) return problem;

        return View(new CategoriesPageViewModel
        {
            Event = ev!,
            NewCategory = new CategoryInputModel { EventId = id }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add([Bind(Prefix = "NewCategory")] CategoryInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError() ?? "Please check the category details.";
            return RedirectToAction(nameof(Index), new { id = model.EventId });
        }

        var result = await Api.PostAsync<CategoryDto>($"api/events/{model.EventId}/categories", ToRequest(model));
        if (result.Unauthorized) return Failed(result, nameof(Index), null, new { id = model.EventId });

        if (result.Success) TempData["Success"] = "Category added.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { id = model.EventId });
    }

    // id = the category's ID
    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem != null) return problem;

        var category = ev!.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { id = eventId });
        }

        return View(new CategoryInputModel
        {
            EventId = eventId,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryInputModel model)
    {
        if (model.CategoryId == null) return RedirectToAction(nameof(Index), new { id = model.EventId });
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PutAsync<CategoryDto>($"api/categories/{model.CategoryId}", ToRequest(model));
        if (result.Unauthorized) return Failed(result, nameof(Index), null, new { id = model.EventId });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save the category.");
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { id = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        if (result.Unauthorized) return Failed(result, nameof(Index), null, new { id = eventId });

        if (result.Success) TempData["Success"] = "Category removed.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { id = eventId });
    }

    private static object ToRequest(CategoryInputModel m) => new
    {
        categoryName = m.CategoryName,
        minAge = m.MinAge,
        maxAge = m.MaxAge
    };
}
