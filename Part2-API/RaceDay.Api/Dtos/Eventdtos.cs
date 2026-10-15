using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Used to create or update an event.</summary>
public class EventRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>1 = Run, 2 = Walk, 3 = Cycle (see GET /api/eventtypes).</summary>
    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string? ImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();

    public static readonly Expression<Func<Event, EventResponse>> Projection = e => new EventResponse
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser!.FullName,
        EventTypeId = e.EventTypeId,
        EventTypeName = e.EventType!.TypeName,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        ImageUrl = e.ImageUrl,
        EnrolmentCount = e.Enrolments.Count(),
        Categories = e.Categories.Select(c => new CategoryResponse
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge
        }).ToList()
    };
}

public class EventTypeResponse
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}