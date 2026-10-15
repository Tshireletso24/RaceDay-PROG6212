using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Creates a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Registering as an Organiser also requires a valid <c>organiserCode</c>, so Participants
    /// cannot give themselves Organiser access. Passwords are hashed with BCrypt before being stored.
    /// </remarks>
    /// <response code="201">Account created. Returns the user (without the password).</response>
    /// <response code="400">Invalid data, invalid role, or invalid organiser code.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Role != Roles.Organiser && request.Role != Roles.Participant)
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        if (request.Role == Roles.Organiser)
        {
            var expected = _config["OrganiserInviteCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserCode != expected)
                return BadRequest(new { message = "A valid organiser invite code is required to register as an Organiser." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", UserResponse.From(user));
    }

    /// <summary>Logs in and starts a session that stores the user's ID and role.</summary>
    /// <response code="200">Logged in. Returns the user and their role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so accounts cannot be probed
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(UserResponse.From(user));
    }

    /// <summary>Ends the current user's session.</summary>
    /// <response code="200">Session cleared.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}