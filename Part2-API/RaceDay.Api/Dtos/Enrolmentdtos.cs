using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentStatusRequest
{
    /// <summary>Pending, Confirmed or Cancelled.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentResponse
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

    public static readonly Expression<Func<Enrolment, EnrolmentResponse>> Projection = e => new EnrolmentResponse
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.EventId,
        EventName = e.Event!.Name,
        EventDate = e.Event!.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.Category!.CategoryName,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant!.FullName,
        Status = e.Status,
        EnrolmentDate = e.EnrolmentDate
    };
}