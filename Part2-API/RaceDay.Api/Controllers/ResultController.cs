using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    private IQueryable<ResultResponse> Project(IQueryable<Result> source) =>
        source.Select(r => new ResultResponse
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            EventName = r.Enrolment!.Event!.Name,
            EventDate = r.Enrolment!.Event!.EventDate,
            CategoryName = r.Enrolment!.Category!.CategoryName,
            FinishTime = r.FinishTime,
            FinishPosition = r.FinishPosition,
            TotalFinishers = _db.Results.Count(x => x.Enrolment!.EventId == r.Enrolment!.EventId)
        });

    /// <summary>Records a finish time and position for an enrolment. Only the owning Organiser can do this.</summary>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data (e.g. FinishTime not in hh:mm:ss format).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture([FromBody] ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only capture results for your own events.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await Project(_db.Results.Where(r => r.ResultId == result.ResultId)).FirstAsync();
        return Created("/api/results/me", response);
    }

    /// <summary>Returns the logged-in Participant's race history, newest first.</summary>
    /// <response code="200">List of results with event, category, finish time, position and total finishers.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = this.CurrentUserId();
        var results = await Project(_db.Results
                .Where(r => r.Enrolment!.ParticipantId == userId)
                .OrderByDescending(r => r.Enrolment!.Event!.EventDate))
            .ToListAsync();
        return Ok(results);
    }
}