namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string FinishTime { get; set; } = string.Empty;   // e.g. "01:45:32"
    public int FinishPosition { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public Enrolment? Enrolment { get; set; }
}