using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.EventDate >= DateTime.Today);
    public int PastEvents => TotalEvents - UpcomingEvents;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);

    public List<EventDto> NextUp => Events
        .Where(e => e.EventDate >= DateTime.Today)
        .OrderBy(e => e.EventDate)
        .Take(3)
        .ToList();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public int? EventTypeId { get; set; }

    /// <summary>The banner already saved for this event (kept unless a new file is uploaded).</summary>
    public string? ExistingImageUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }
}

public class CategoryInputModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public CategoryInputModel NewCategory { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();

    public int Confirmed => Enrolments.Count(e => e.Status == "Confirmed");
    public int Pending => Enrolments.Count(e => e.Status == "Pending");
    public int Cancelled => Enrolments.Count(e => e.Status == "Cancelled");
}

public class ResultInputModel
{
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    [Required(ErrorMessage = "Please enter the finish time.")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Finish time must look like 01:45:32 (hh:mm:ss).")]
    public string FinishTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finishing position.")]
    [Range(1, 1000000, ErrorMessage = "Finishing position must be 1 or higher.")]
    public int? FinishPosition { get; set; }
}

public class ResultsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();

    public int Published => Enrolments.Count(e => e.HasResult);
}
