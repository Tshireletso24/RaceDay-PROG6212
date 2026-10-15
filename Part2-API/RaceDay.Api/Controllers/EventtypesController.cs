using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/eventtypes")]
public class EventTypesController : ControllerBase
{
	private readonly RaceDayDbContext _db;

	public EventTypesController(RaceDayDbContext db) => _db = db;

	/// <summary>Lists the event types (Run, Walk, Cycle). Public.</summary>
	/// <response code="200">List of event types.</response>
	[HttpGet]
	[ProducesResponseType(typeof(List<EventTypeResponse>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll()
	{
		var types = await _db.EventTypes
			.OrderBy(t => t.EventTypeId)
			.Select(t => new EventTypeResponse { EventTypeId = t.EventTypeId, TypeName = t.TypeName })
			.ToListAsync();
		return Ok(types);
	}
}