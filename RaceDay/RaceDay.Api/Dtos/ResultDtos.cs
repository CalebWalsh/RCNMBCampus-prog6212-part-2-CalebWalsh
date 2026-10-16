using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    [Required]
    public TimeSpan? FinishTime { get; set; }

    [Required, Range(1, 1000000)]
    public int? Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime CapturedDate { get; set; }
}
