using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("User")]
public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Role { get; set; }
    public string? IdNumberOrPassport { get; set; }
    public string? AsaPermanentLicenseNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Participant? Participant { get; set; }
    public Organiser? Organiser { get; set; }
}
