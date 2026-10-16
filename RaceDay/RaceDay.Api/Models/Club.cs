namespace RaceDay.Api.Models;

public class Club
{
    public int ClubID { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateTime? DateEstablished { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
