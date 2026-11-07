namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string IdNumberOrPassport { get; set; }
    public required string Role { get; set; }
    public string? AsaPermanentLicenseNumber { get; set; }
}

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Role { get; set; }
    public string? IdNumberOrPassport { get; set; }
    public string? AsaPermanentLicenseNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? IdNumberOrPassport { get; set; }
    public string? AsaPermanentLicenseNumber { get; set; }
}
