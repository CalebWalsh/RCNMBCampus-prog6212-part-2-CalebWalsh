using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EventsController : ApiControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpGet("events")]
    [SwaggerOperation(Summary = "List upcoming events", Description = "Public. Returns all events dated today or later, soonest first. Expects no body. Returns 200 with the list of events.")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcoming()
    {
        var today = DateTime.Today;
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(EventResponse.From));
    }

    [HttpGet("events/{id:int}")]
    [SwaggerOperation(Summary = "View one event", Description = "Public. Returns the full details of the event with the given id. Returns 200 with the event, or 404 if it does not exist.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null)
            return NotFound(new { message = "Event does not exist." });

        return Ok(EventResponse.From(ev));
    }

    [HttpPost("events")]
    [SwaggerOperation(Summary = "Create an event", Description = "Organiser only. Expects eventName, description, eventDate, location, distance and eventType (Running, Walking or Cycling). The logged-in Organiser becomes the owner. Returns 201 with the event, 400 for invalid data, 401 if not logged in, or 403 if not an Organiser.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var ev = new Event
        {
            EventName = request.EventName,
            Description = request.Description,
            EventDate = request.EventDate!.Value.Date,
            Location = request.Location,
            Distance = request.Distance!.Value,
            EventType = request.EventType,
            OrganiserID = CurrentUserId,
            DateCreated = DateTime.Now
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, EventResponse.From(ev));
    }

    [HttpPut("events/{id:int}")]
    [SwaggerOperation(Summary = "Update an event", Description = "Organiser only, and only the Organiser who owns the event. Expects the same fields as creating an event. Returns 200 with the updated event, 400 for invalid data, 401, 403 if not the owner, or 404 if the event does not exist.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null)
            return NotFound(new { message = "Event does not exist." });

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can update it.");

        ev.EventName = request.EventName;
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value.Date;
        ev.Location = request.Location;
        ev.Distance = request.Distance!.Value;
        ev.EventType = request.EventType;
        await _db.SaveChangesAsync();

        return Ok(EventResponse.From(ev));
    }

    [HttpDelete("events/{id:int}")]
    [SwaggerOperation(Summary = "Delete an event", Description = "Organiser only, and only the owning Organiser. Also removes the event's categories, enrolments and results. Expects no body. Returns 204, 401, 403 if not the owner, or 404 if the event does not exist.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null)
            return NotFound(new { message = "Event does not exist." });

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can delete it.");

        var enrolments = await _db.Enrolments.Include(e => e.Result).Where(e => e.EventID == id).ToListAsync();
        var categories = await _db.Categories.Where(c => c.EventID == id).ToListAsync();

        _db.Results.RemoveRange(enrolments.Where(e => e.Result != null).Select(e => e.Result!));
        _db.Enrolments.RemoveRange(enrolments);
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
