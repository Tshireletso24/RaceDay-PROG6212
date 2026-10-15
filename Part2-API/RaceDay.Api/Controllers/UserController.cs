using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.FindAsync(this.CurrentUserId());
        if (user == null) return NotFound(new { message = "User not found." });
        return Ok(UserResponse.From(user));
    }

    /// <summary>Updates the logged-in user's own name, phone number and profile picture URL.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(this.CurrentUserId());
        if (user == null) return NotFound(new { message = "User not found." });

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await _db.SaveChangesAsync();

        return Ok(UserResponse.From(user));
    }
}