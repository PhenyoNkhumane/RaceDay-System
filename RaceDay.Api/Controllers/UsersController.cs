using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    [RequireLogin]
    [EndpointSummary("Get the signed-in user's profile")]
    [EndpointDescription("Returns the current user's profile, including their role and personal details; password hashes are never returned. Requires an authenticated session.")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMe()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        var user = userId is null ? null : _db.Users.Find(userId.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("me")]
    [RequireLogin]
    [EndpointSummary("Update the signed-in user's profile")]
    [EndpointDescription("Supply any profile fields to update: firstName, lastName, email, idNumberOrPassport, or asaPermanentLicenseNumber. Omitted fields are unchanged. Returns the updated profile, or 400 if the email is already in use. Requires an authenticated session.")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult UpdateMe(UpdateProfileDto dto)
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        var user = userId is null ? null : _db.Users.Find(userId.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        if (dto.Email is not null &&
            _db.Users.Any(u => u.Email == dto.Email && u.Id != user.Id))
        {
            return BadRequest("Email is already registered.");
        }

        user.FirstName = dto.FirstName ?? user.FirstName;
        user.LastName = dto.LastName ?? user.LastName;
        user.Email = dto.Email ?? user.Email;
        user.IdNumberOrPassport = dto.IdNumberOrPassport ?? user.IdNumberOrPassport;
        user.AsaPermanentLicenseNumber = dto.AsaPermanentLicenseNumber ?? user.AsaPermanentLicenseNumber;
        _db.SaveChanges();
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpGet("me/memberships")]
    [RequireRole(Roles.Participant)]
    [EndpointSummary("List the signed-in participant's club memberships")]
    [EndpointDescription("Returns the current Participant's club memberships with club, discipline, membership number, join date and status. Participants only; requires an authenticated session.")]
    [ProducesResponseType(typeof(List<MembershipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetMyMemberships()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var memberships = _db.ClubMemberships.Where(m => m.ParticipantId == userId)
            .Select(membership => new MembershipResponseDto
            {
                Id = membership.Id,
                ClubId = membership.ClubId,
                DisciplineId = membership.DisciplineId,
                MembershipNumber = membership.MembershipNumber,
                JoinedAt = membership.JoinedAt,
                Status = membership.Status
            }).ToList();
        return Ok(memberships);
    }
}
