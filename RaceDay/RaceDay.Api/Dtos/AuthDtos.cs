using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public int? ClubId { get; set; }
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public int? ClubId { get; set; }
}

public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int? ClubId { get; set; }
    public DateTime DateRegistered { get; set; }

    public static UserResponse From(User user) => new()
    {
        UserId = user.UserID,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        PhoneNumber = user.PhoneNumber,
        ClubId = user.ClubID,
        DateRegistered = user.DateRegistered
    };
}
