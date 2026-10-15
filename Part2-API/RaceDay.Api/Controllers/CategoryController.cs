using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
	private readonly RaceDayDbContext _db;

	public CategoriesController(RaceDayDbContext db) => _db = db;

	/// <summary>Lists the categories available for an event. Public.</summary>
	/// <response code="200">List of categories.</response>
	/// <response code="404">Event not found.</response>
	[HttpGet("events/{eventId:int}/categories")]
	[ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetForEvent(int eventId)
	{
		if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
			return NotFound(new { message = "Event not found." });

		var categories = await _db.Categories
			.Where(c => c.EventId == eventId)
			.OrderBy(c => c.CategoryId)
			.Select(CategoryResponse.Projection)
			.ToListAsync();
		return Ok(categories);
	}

	/// <summary>Adds an age or distance category to an event. Only the owning Organiser can do this.</summary>
	/// <response code="201">Category created.</response>
	/// <response code="400">Invalid data (e.g. MinAge greater than MaxAge).</response>
	/// <response code="401">Not logged in.</response>
	/// <response code="403">Not an Organiser, or not the owner of this event.</response>
	/// <response code="404">Event not found.</response>
	[HttpPost("events/{eventId:int}/categories")]
	[SessionAuthorize(Roles.Organiser)]
	[ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
	{
		var ev = await _db.Events.FindAsync(eventId);
		if (ev == null) return NotFound(new { message = "Event not found." });
		if (ev.OrganiserId != this.CurrentUserId())
			return this.ForbiddenResult("You can only add categories to your own events.");
		if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
			return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

		var category = new Category
		{
			EventId = eventId,
			CategoryName = request.CategoryName.Trim(),
			MinAge = request.MinAge,
			MaxAge = request.MaxAge
		};
		_db.Categories.Add(category);
		await _db.SaveChangesAsync();

		var response = await _db.Categories.Where(c => c.CategoryId == category.CategoryId).Select(CategoryResponse.Projection).FirstAsync();
		return Created($"/api/events/{eventId}/categories", response);
	}

	/// <summary>Updates a category. Only the Organiser who owns the event can do this.</summary>
	/// <response code="200">The updated category.</response>
	/// <response code="400">Invalid data.</response>
	/// <response code="401">Not logged in.</response>
	/// <response code="403">Not an Organiser, or not the owner of this event.</response>
	/// <response code="404">Category not found.</response>
	[HttpPut("categories/{id:int}")]
	[SessionAuthorize(Roles.Organiser)]
	[ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
	{
		var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
		if (category == null) return NotFound(new { message = "Category not found." });
		if (category.Event!.OrganiserId != this.CurrentUserId())
			return this.ForbiddenResult("You can only change categories on your own events.");
		if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
			return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

		category.CategoryName = request.CategoryName.Trim();
		category.MinAge = request.MinAge;
		category.MaxAge = request.MaxAge;
		await _db.SaveChangesAsync();

		var response = await _db.Categories.Where(c => c.CategoryId == id).Select(CategoryResponse.Projection).FirstAsync();
		return Ok(response);
	}

	/// <summary>Deletes a category. Only the owning Organiser can do this, and only if nobody has enrolled in it.</summary>
	/// <response code="204">Category deleted.</response>
	/// <response code="401">Not logged in.</response>
	/// <response code="403">Not an Organiser, or not the owner of this event.</response>
	/// <response code="404">Category not found.</response>
	/// <response code="409">Participants are already enrolled in this category.</response>
	[HttpDelete("categories/{id:int}")]
	[SessionAuthorize(Roles.Organiser)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete(int id)
	{
		var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
		if (category == null) return NotFound(new { message = "Category not found." });
		if (category.Event!.OrganiserId != this.CurrentUserId())
			return this.ForbiddenResult("You can only delete categories on your own events.");
		if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
			return Conflict(new { message = "Participants are already enrolled in this category." });

		_db.Categories.Remove(category);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}