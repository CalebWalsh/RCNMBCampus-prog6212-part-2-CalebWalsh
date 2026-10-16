using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

public class UsersController : ApiControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpGet("users/me")]
    [SwaggerOperation(Summary = "View my profile", Description = "Returns the profile of the logged-in user (either role). Expects no body. Returns 200 with the profile, or 401 if not logged in.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SessionAuthorize]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user is null)
            return Unauthorized(new { message = "User no longer exists." });

        return Ok(UserResponse.From(user));
    }

    [HttpPut("users/me")]
    [SwaggerOperation(Summary = "Update my profile", Description = "Updates the logged-in user's fullName, phoneNumber and clubId. Returns 200 with the updated profile, 400 for invalid data or an unknown club, or 401 if not logged in.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SessionAuthorize]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user is null)
            return Unauthorized(new { message = "User no longer exists." });

        if (request.ClubId.HasValue && !await _db.Clubs.AnyAsync(c => c.ClubID == request.ClubId.Value))
            return BadRequest(new { message = "Club does not exist." });

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.ClubID = request.ClubId;
        await _db.SaveChangesAsync();

        return Ok(UserResponse.From(user));
    }
}
