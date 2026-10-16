using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db)
    {
        _db = db;
    }

    [HttpGet("events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "List an event's categories", Description = "Public. Returns the categories (such as 10km, 21km or Under 20) defined for the event. Returns 200 with the list, or 404 if the event does not exist.")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event does not exist." });

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.From));
    }

    [HttpPost("events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "Add a category to an event", Description = "Organiser only, and only the owning Organiser. Expects categoryName, minAge, maxParticipants and entryFee. Returns 201 with the category, 400 for invalid data or a duplicate name, 401, 403 if not the owner, or 404 if the event does not exist.")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
            return NotFound(new { message = "Event does not exist." });

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can add categories.");

        if (await _db.Categories.AnyAsync(c => c.EventID == eventId && c.CategoryName == request.CategoryName))
            return BadRequest(new { message = "This event already has a category with that name." });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName,
            MinAge = request.MinAge,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryResponse.From(category));
    }

    [HttpPut("categories/{id:int}")]
    [SwaggerOperation(Summary = "Update a category", Description = "Organiser only, and only the Organiser who owns the event. Expects categoryName, minAge, maxParticipants and entryFee. Returns 200 with the category, 400 for invalid data or a duplicate name, 401, 403, or 404 if the category does not exist.")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null)
            return NotFound(new { message = "Category does not exist." });

        if (category.Event.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can update its categories.");

        if (await _db.Categories.AnyAsync(c => c.EventID == category.EventID && c.CategoryID != id && c.CategoryName == request.CategoryName))
            return BadRequest(new { message = "This event already has a category with that name." });

        category.CategoryName = request.CategoryName;
        category.MinAge = request.MinAge;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;
        await _db.SaveChangesAsync();

        return Ok(CategoryResponse.From(category));
    }

    [HttpDelete("categories/{id:int}")]
    [SwaggerOperation(Summary = "Delete a category", Description = "Organiser only, and only the Organiser who owns the event. Expects no body. Returns 204, 401, 403, 404 if the category does not exist, or 409 if participants are enrolled in it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null)
            return NotFound(new { message = "Category does not exist." });

        if (category.Event.OrganiserID != CurrentUserId)
            return Forbidden("Only the organiser who created this event can delete its categories.");

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
            return Conflict(new { message = "Category has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
