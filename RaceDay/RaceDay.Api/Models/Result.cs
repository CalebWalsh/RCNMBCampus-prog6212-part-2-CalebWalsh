namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime CapturedDate { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
}
