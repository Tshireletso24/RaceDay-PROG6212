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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant in an event under a chosen category.</summary>
    /// <remarks>New enrolments start with the status Pending.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Event or category not found, category not part of the event, or the event has passed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol([FromBody] EnrolmentRequest request)
    {
        var userId = this.CurrentUserId();

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (ev == null)
            return BadRequest(new { message = "Event not found." });
        if (ev.EventDate < DateTime.UtcNow)
            return BadRequest(new { message = "This event has already taken place." });

        var categoryOk = await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (!categoryOk)
            return BadRequest(new { message = "Category not found for this event." });

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.EventId == request.EventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            Status = EnrolmentStatuses.Pending
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments.Where(e => e.EnrolmentId == enrolment.EnrolmentId).Select(EnrolmentResponse.Projection).FirstAsync();
        return Created("/api/enrolments/me", response);
    }

    /// <summary>Lists every event the logged-in Participant has enrolled in, with status.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = this.CurrentUserId();
        var enrolments = await _db.Enrolments
            .Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.Event!.EventDate)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
        return Ok(enrolments);
    }

    /// <summary>Lists all Participants enrolled in an event. Only the owning Organiser can do this.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only view enrolments for your own events.");

        var enrolments = await _db.Enrolments
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
        return Ok(enrolments);
    }

    /// <summary>Sets an enrolment's status (Pending, Confirmed or Cancelled). Only the owning Organiser can do this.</summary>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="400">Invalid status.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpPut("enrolments/{id:int}/status")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] EnrolmentStatusRequest request)
    {
        var allowed = new[] { EnrolmentStatuses.Pending, EnrolmentStatuses.Confirmed, EnrolmentStatuses.Cancelled };
        if (!allowed.Contains(request.Status))
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });

        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only manage enrolments for your own events.");

        enrolment.Status = request.Status;
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments.Where(e => e.EnrolmentId == id).Select(EnrolmentResponse.Projection).FirstAsync();
        return Ok(response);
    }
}