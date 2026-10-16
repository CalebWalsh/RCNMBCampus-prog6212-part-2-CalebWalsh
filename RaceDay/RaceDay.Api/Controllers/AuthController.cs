using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace RaceDay.Api.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly RaceDayContext _db;

    public AuthController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpPost("auth/register")]
    [SwaggerOperation(Summary = "Register a new account", Description = "Creates an Organiser or Participant account. Expects fullName, email, password (minimum 8 characters), role (Organiser or Participant) and optionally phoneNumber and clubId. Returns 201 with the new user, 400 for invalid data or an unknown club, or 409 if the email is already registered.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email already registered." });

        if (request.ClubId.HasValue && !await _db.Clubs.AnyAsync(c => c.ClubID == request.ClubId.Value))
            return BadRequest(new { message = "Club does not exist." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            ClubID = request.ClubId,
            DateRegistered = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserResponse.From(user));
    }

    [HttpPost("auth/login")]
    [SwaggerOperation(Summary = "Log in and start a session", Description = "Checks the credentials and starts a server-side session that stores the user id and role. Expects email and password. Returns 200 with the user's details and sets the session cookie, or 401 for invalid credentials. Log in here first before testing protected endpoints.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordMatches(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(UserResponse.From(user));
    }

    [HttpPost("auth/logout")]
    [SwaggerOperation(Summary = "Log out", Description = "Ends the current session. Expects no body. Returns 204, or 401 if nobody is logged in.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SessionAuthorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    private static bool PasswordMatches(string password, string hash)
    {
        try
        {
            return BCryptNet.Verify(password, hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
