using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultsController : ApiControllerBase
{
    private static readonly Expression<Func<Result, ResultResponse>> ToResponse = r => new ResultResponse
    {
        ResultId = r.ResultID,
        EnrolmentId = r.EnrolmentID,
        EventId = r.Enrolment.EventID,
        EventName = r.Enrolment.Event.EventName,
        ParticipantName = r.Enrolment.Participant.FullName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        CapturedDate = r.CapturedDate
    };

    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SwaggerOperation(Summary = "Capture a result", Description = "Organiser only, and only for events they own. Expects finishTime (hh:mm:ss) and position. Returns 201 with the result, 400 for invalid data, 401, 403 if not the event owner, 404 if the enrolment does not exist, or 409 if a result was already captured.")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Capture(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
        if (enrolment is null)
            return NotFound(new { message = "Enrolment does not exist." });

        if (enrolment.Event.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser of this event can capture results.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            CapturedDate = DateTime.Now
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await _db.Results
            .Where(r => r.ResultID == result.ResultID)
            .Select(ToResponse)
            .SingleAsync();

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("users/me/results")]
    [SwaggerOperation(Summary = "View my results", Description = "Participant only. Returns the logged-in Participant's finish times and positions across all events, newest event first. Returns 200 with the list, 401, or 403 if not a Participant.")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SessionAuthorize(Role = Roles.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var results = await _db.Results
            .Where(r => r.Enrolment.ParticipantID == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("events/{eventId:int}/results")]
    [SwaggerOperation(Summary = "View an event's results", Description = "Any logged-in user. Returns all captured results for the event ordered by finishing position. Returns 200 with the list, 401 if not logged in, or 404 if the event does not exist.")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event does not exist." });

        var results = await _db.Results
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(results);
    }
}
