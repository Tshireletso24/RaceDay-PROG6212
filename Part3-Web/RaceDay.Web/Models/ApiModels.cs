namespace RaceDay.Web.Models;

// These classes mirror the JSON the RaceDay API returns.

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}

public class EventTypeDto
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}

public class EventDto
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
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public bool HasResult { get; set; }
}

public class ResultDto
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
