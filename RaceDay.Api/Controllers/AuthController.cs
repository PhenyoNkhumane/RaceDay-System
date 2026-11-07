using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [EndpointSummary("Register a user and start an authenticated session")]
    [EndpointDescription("Creates a Participant or Organiser account. Supply email, a password of at least 12 characters with mixed case, a number and a symbol, firstName, lastName, idNumberOrPassport, and role (Participant or Organiser); asaPermanentLicenseNumber is optional. Organiser registration also requires organiserCode. Returns 201 with the new user profile and sets the session cookie. Passwords are stored as hashes.")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!Roles.Allowed.Contains(dto.Role))
        {
            return BadRequest("Invalid role.");
        }

        if (dto.Role == Roles.Organiser &&
            dto.OrganiserCode != _configuration["Registration:OrganiserCode"])
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid organiser code.");
        }

        if (!IsStrongPassword(dto.Password))
        {
            return BadRequest("Password must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters.");
        }

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("Email is already registered.");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role,
            IdNumberOrPassport = dto.IdNumberOrPassport,
            AsaPermanentLicenseNumber = dto.AsaPermanentLicenseNumber,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        if (user.Role == Roles.Participant)
        {
            _db.Participants.Add(new Participant { UserId = user.Id });
        }
        else
        {
            _db.Organisers.Add(new Organiser
            {
                UserId = user.Id,
                ProvisionedBy = "Self-registration",
                ProvisionedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Registering also logs the user in: store ID and role in the server-side session.
        CurrentUser.SignIn(HttpContext, user.Id, user.Role);
        return Created("/api/users/me", UserMapper.ToResponse(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Log in and start an authenticated session")]
    [EndpointDescription("Supply email and password. Returns 200 with the user's profile and sets the server-side session cookie, or 401 for invalid credentials.")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized("Invalid email or password");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        CurrentUser.SignIn(HttpContext, user.Id, user.Role);
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPost("logout")]
    [EndpointSummary("Log out and clear the session")]
    [EndpointDescription("Clears the current server-side session and expires the session cookie. Returns 204 No Content.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        CurrentUser.SignOut(HttpContext);
        return NoContent();
    }

    [HttpPut("password")]
    [RequireLogin]
    [EndpointSummary("Change the signed-in user's password")]
    [EndpointDescription("Requires the currentPassword and a strong newPassword. Returns 204 when changed, 400 for a weak password, or 401 when the current password is invalid or the session is missing.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!IsStrongPassword(dto.NewPassword))
        {
            return BadRequest("Password must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsStrongPassword(string password) =>
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));
}
