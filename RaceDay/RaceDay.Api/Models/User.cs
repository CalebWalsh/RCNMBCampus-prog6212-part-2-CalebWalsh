namespace RaceDay.Api.Models;

public class User
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int? ClubID { get; set; }
    public DateTime DateRegistered { get; set; }
    public Club? Club { get; set; }
}
