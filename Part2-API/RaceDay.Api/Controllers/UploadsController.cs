using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Image uploads. Files go to Azure Blob Storage; the URL is saved in the database.</summary>
[ApiController]
[Route("api")]
public class UploadsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(RaceDayDbContext db, IBlobStorageService blobs, ILogger<UploadsController> logger)
    {
        _db = db;
        _blobs = blobs;
        _logger = logger;
    }

    /// <summary>Uploads a banner image for an event. Only the owning Organiser can do this.</summary>
    /// <remarks>Send as multipart/form-data with a file field named <c>file</c> (JPG, PNG, WebP or GIF, max 5 MB).</remarks>
    /// <response code="200">The updated event, including the banner URL.</response>
    /// <response code="400">No file, wrong file type, or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [HttpPost("events/{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != this.CurrentUserId())
            return this.ForbiddenResult("You can only upload banners for your own events.");

        var error = ImageValidator.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        try
        {
            await using var stream = file.OpenReadStream();
            ev.ImageUrl = await _blobs.UploadImageAsync(stream, file.FileName, file.ContentType, "events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Banner upload failed for event {EventId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is unavailable. Please try again later." });
        }

        await _db.SaveChangesAsync();

        var response = await _db.Events.Where(e => e.EventId == id).Select(EventResponse.Projection).FirstAsync();
        return Ok(response);
    }

    /// <summary>Uploads a profile picture for the logged-in user.</summary>
    /// <remarks>Send as multipart/form-data with a file field named <c>file</c> (JPG, PNG, WebP or GIF, max 5 MB).</remarks>
    /// <response code="200">The updated profile, including the picture URL.</response>
    /// <response code="400">No file, wrong file type, or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [HttpPost("users/me/picture")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var user = await _db.Users.FindAsync(this.CurrentUserId());
        if (user == null) return NotFound(new { message = "User not found." });

        var error = ImageValidator.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        try
        {
            await using var stream = file.OpenReadStream();
            user.ProfilePictureUrl = await _blobs.UploadImageAsync(stream, file.FileName, file.ContentType, "profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile picture upload failed for user {UserId}", user.UserId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is unavailable. Please try again later." });
        }

        await _db.SaveChangesAsync();
        return Ok(UserResponse.From(user));
    }
}
