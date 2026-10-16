using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrolmentsController : ApiControllerBase
{
    private static readonly Expression<Func<Enrolment, EnrolmentResponse>> ToResponse = e => new EnrolmentResponse
    {
        EnrolmentId = e.EnrolmentID,
        ParticipantId = e.ParticipantID,
        ParticipantName = e.Participant.FullName,
        EventId = e.EventID,
        EventName = e.Event.EventName,
        CategoryId = e.CategoryID,
        CategoryName = e.Category.CategoryName,
        EnrolmentDate = e.EnrolmentDate
    };

    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpPost("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "Enter an event", Description = "Participant only. Expects categoryId for a category of this event. Records the link between the logged-in Participant, the event and the category. Returns 201 with the enrolment, 400 if the category belongs to another event, 401, 403 if not a Participant, 404 if the event or category does not exist, or 409 if already enrolled or the category is full.")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SessionAuthorize(Role = Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, EnrolRequest request)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event does not exist." });

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId!.Value);
        if (category is null)
            return NotFound(new { message = "Category does not exist." });

        if (category.EventID != eventId)
            return BadRequest(new { message = "Category does not belong to this event." });

        if (await _db.Enrolments.AnyAsync(e => e.EventID == eventId && e.ParticipantID == CurrentUserId))
            return Conflict(new { message = "You are already enrolled in this event." });

        if (category.MaxParticipants.HasValue)
        {
            var taken = await _db.Enrolments.CountAsync(e => e.CategoryID == category.CategoryID);
            if (taken >= category.MaxParticipants.Value)
                return Conflict(new { message = "This category is full." });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = CurrentUserId,
            EventID = eventId,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.Now
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments
            .Where(e => e.EnrolmentID == enrolment.EnrolmentID)
            .Select(ToResponse)
            .SingleAsync();

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("users/me/enrolments")]
    [SwaggerOperation(Summary = "View my enrolments", Description = "Participant only. Returns every event the logged-in Participant has entered, with the chosen category. Returns 200 with the list, 401, or 403 if not a Participant.")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SessionAuthorize(Role = Roles.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var enrolments = await _db.Enrolments
            .Where(e => e.ParticipantID == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(enrolments);
    }

    [HttpGet("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "View all enrolments for an event", Description = "Organiser only, and only the owning Organiser. Returns every Participant enrolled in the event. Returns 200 with the list, 401, 403 if not the owner, or 404 if the event does not exist.")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
            return NotFound(new { message = "Event does not exist." });

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can view its enrolments.");

        var enrolments = await _db.Enrolments
            .Where(e => e.EventID == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(enrolments);
    }

    [HttpDelete("enrolments/{id:int}")]
    [SwaggerOperation(Summary = "Cancel my enrolment", Description = "Participant only, and only for their own enrolment. Any captured result for it is removed too. Returns 204, 401, 403 if it is not their enrolment, or 404 if it does not exist.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment is null)
            return NotFound(new { message = "Enrolment does not exist." });

        if (enrolment.ParticipantID != CurrentUserId)
            return Forbidden("You can only cancel your own enrolment.");

        if (enrolment.Result is not null)
            _db.Results.Remove(enrolment.Result);

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
