using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time as hh:mm:ss, e.g. 01:45:32.</summary>
    [Required, RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "FinishTime must be in the format hh:mm:ss.")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 1000000)]
    public int FinishPosition { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int FinishPosition { get; set; }

    /// <summary>Number of recorded finishers for the event (for "47th out of 312").</summary>
    public int TotalFinishers { get; set; }
}