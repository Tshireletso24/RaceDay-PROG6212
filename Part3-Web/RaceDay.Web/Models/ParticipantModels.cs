using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---- Shapes returned by the API for the Participant pages ----

public class BrowseCategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}

public class BrowseEventDto
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
    public List<BrowseCategoryDto> Categories { get; set; } = new();
}

public class MyEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public bool HasResult => FinishPosition.HasValue;
}

public class MyResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int FinishPosition { get; set; }
    public int TotalFinishers { get; set; }
}

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// ---- View models ----

public class HomeViewModel
{
    public List<BrowseEventDto> Events { get; set; } = new();
    public List<EventTypeDto> EventTypes { get; set; } = new();
    public int? EventTypeId { get; set; }
    public string? Search { get; set; }
    public bool IncludePast { get; set; }
    public string? LoadError { get; set; }

    public bool HasFilters => EventTypeId.HasValue || !string.IsNullOrWhiteSpace(Search) || IncludePast;
}

public class EventDetailsViewModel
{
    public BrowseEventDto Event { get; set; } = new();
    public string? Role { get; set; }
    public MyEnrolmentDto? MyEnrolment { get; set; }

    public bool IsLoggedIn => Role != null;
    public bool IsParticipant => Role == "Participant";
    public bool HasPassed => Event.EventDate < DateTime.Now;
}

public class MyEventsViewModel
{
    public List<MyEnrolmentDto> Upcoming { get; set; } = new();
    public List<MyEnrolmentDto> Past { get; set; } = new();
}

public class MyResultsViewModel
{
    public List<MyResultDto> Results { get; set; } = new();
    public int Podiums => Results.Count(r => r.FinishPosition <= 3);
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // Read-only, filled from the API
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>Small formatting helpers shared by the Participant pages.</summary>
public static class Formatting
{
    public static string Ordinal(int n)
    {
        var last2 = n % 100;
        if (last2 >= 11 && last2 <= 13) return $"{n}th";
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string AgeRange(int? min, int? max)
    {
        if (min.HasValue && max.HasValue) return $"Ages {min} to {max}";
        if (min.HasValue) return $"Ages {min} and over";
        if (max.HasValue) return $"Up to age {max}";
        return "All ages";
    }
}
