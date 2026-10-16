using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.01, 9999.99)]
    public decimal? Distance { get; set; }

    [Required, RegularExpression("^(Running|Walking|Cycling)$", ErrorMessage = "EventType must be Running, Walking or Cycling.")]
    public string EventType { get; set; } = string.Empty;
}

public class EventResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public DateTime DateCreated { get; set; }

    public static EventResponse From(Event ev) => new()
    {
        EventId = ev.EventID,
        EventName = ev.EventName,
        Description = ev.Description,
        EventDate = ev.EventDate,
        Location = ev.Location,
        Distance = ev.Distance,
        EventType = ev.EventType,
        OrganiserId = ev.OrganiserID,
        DateCreated = ev.DateCreated
    };
}

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }

    [Range(0, 99999.99)]
    public decimal EntryFee { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public static CategoryResponse From(Category category) => new()
    {
        CategoryId = category.CategoryID,
        EventId = category.EventID,
        CategoryName = category.CategoryName,
        MinAge = category.MinAge,
        MaxParticipants = category.MaxParticipants,
        EntryFee = category.EntryFee
    };
}
