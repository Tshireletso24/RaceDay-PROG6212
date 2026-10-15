using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists upcoming events. Public.</summary>
    /// <remarks>
    /// Optional filters: <c>eventTypeId</c>, <c>includePast</c> (also show past events).
    /// An Organiser can pass <c>mine=true</c> to see all of their own events, past and upcoming.
    /// </remarks>
    /// <response code="200">List of events with their categories and enrolment counts.</response>
    /// <response code="401">mine=true was used without being logged in.</response>
    /// <response code="403">mine=true was used by a non-Organiser.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEvents([FromQuery] int? eventTypeId, [FromQuery] bool includePast = false, [FromQuery] bool mine = false)
    {
        var query = _db.Events.AsQueryable();

        if (mine)
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });
            if (HttpContext.Session.GetString(SessionKeys.Role) != Roles.Organiser)
                return this.ForbiddenResult("This action requires the Organiser role.");

            query = query.Where(e => e.OrganiserId == userId.Value);
        }
        else if (!includePast)
        {
            query = query.Where(e => e.EventDate >= DateTime.UtcNow.Date);
        }

        if (eventTypeId.HasValue)
            query = query.Where(e => e.EventTypeId == eventTypeId.Value);

        var events = await query.OrderBy(e => e.EventDate).Select(EventResponse.Projection).ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns full details for one event, including its categories. Public.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.Where(e => e.EventId == id).Select(EventResponse.Projection).FirstOrDefaultAsync();
        if (ev == null) return NotFound(new { message = "Event not found." });
        return Ok(ev);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data or event type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
            return BadRequest(new { message = "Invalid eventTypeId." });

        var ev = new Event
        {
            OrganiserId = this.CurrentUserId(),
            EventTypeId = request.EventTypeId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            ImageUrl = request.ImageUrl
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var response = await _db.Events.Where(e => e.EventId == ev.EventId).Select(EventResponse.Projection).FirstAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, response);
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Invalid data or event type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only change your own events.");

        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
            return BadRequest(new { message = "Invalid eventTypeId." });

        ev.EventTypeId = request.EventTypeId;
        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.ImageUrl = request.ImageUrl;
        await _db.SaveChangesAsync();

        var response = await _db.Events.Where(e => e.EventId == id).Select(EventResponse.Projection).FirstAsync();
        return Ok(response);
    }

    /// <summary>Deletes an event and its categories and enrolments. Only the owning Organiser can do this.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only delete your own events.");

        // Remove enrolments first so category deletion is never blocked by them
        var enrolments = await _db.Enrolments.Where(e => e.EventId == id).ToListAsync();
        _db.Enrolments.RemoveRange(enrolments);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}