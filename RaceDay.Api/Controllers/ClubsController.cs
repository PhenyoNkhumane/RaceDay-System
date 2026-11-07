using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClubsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ClubsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [EndpointSummary("List clubs")]
    [EndpointDescription("Returns a JSON array of available clubs with their contact and registration details. No request body is required.")]
    [ProducesResponseType(typeof(List<ClubResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetClubs()
    {
        return Ok(_db.Clubs.Select(club => new ClubResponseDto
        {
            Id = club.Id,
            Name = club.Name,
            RegistrationNumber = club.RegistrationNumber,
            ContactEmail = club.ContactEmail,
            ContactNumber = club.ContactNumber,
            CreatedAt = club.CreatedAt
        }).ToList());
    }

    [HttpGet("{clubId:int}")]
    [EndpointSummary("Get a club")]
    [EndpointDescription("Returns the club's name, registration and contact details. Returns 404 if the club does not exist.")]
    [ProducesResponseType(typeof(ClubResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetClub(int clubId)
    {
        var club = _db.Clubs.Find(clubId);
        if (club is null)
        {
            return NotFound();
        }

        return Ok(ResponseMapper.ToResponse(club));
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("Create a club")]
    [EndpointDescription("Organisers only. Supply name and optional registrationNumber, contactEmail and contactNumber. Returns 201 with the created club, or 409 if its name or registration number is already used.")]
    [ProducesResponseType(typeof(ClubResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CreateClub(CreateClubDto dto)
    {
        if (_db.Clubs.Any(c => c.Name == dto.Name ||
            (dto.RegistrationNumber != null && c.RegistrationNumber == dto.RegistrationNumber)))
        {
            return Conflict("A club with this name or registration number already exists.");
        }

        var club = new Club
        {
            Name = dto.Name,
            RegistrationNumber = dto.RegistrationNumber,
            ContactEmail = dto.ContactEmail,
            ContactNumber = dto.ContactNumber,
            CreatedAt = DateTime.UtcNow
        };
        _db.Clubs.Add(club);
        _db.SaveChanges();
        return Created($"/api/clubs/{club.Id}", ResponseMapper.ToResponse(club));
    }

    [HttpPost("{clubId:int}/memberships")]
    [RequireRole(Roles.Participant)]
    [EndpointSummary("Join a club")]
    [EndpointDescription("Participants only. Supply disciplineId and optional membershipNumber and joinedAt. Returns 201 with the new membership, 404 if the club or discipline is missing, or 409 if already a member of a club for that discipline.")]
    [ProducesResponseType(typeof(MembershipResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult JoinClub(int clubId, CreateMembershipDto dto)
    {
        if (!_db.Clubs.Any(c => c.Id == clubId) || !_db.Disciplines.Any(d => d.Id == dto.DisciplineId))
        {
            return NotFound();
        }

        var participantId = CurrentUser.GetUserId(HttpContext)!.Value;
        if (_db.ClubMemberships.Any(m => m.ParticipantId == participantId && m.DisciplineId == dto.DisciplineId))
        {
            return Conflict("Already a member of a club for this discipline.");
        }

        var membership = new ClubMembership
        {
            ParticipantId = participantId,
            ClubId = clubId,
            DisciplineId = dto.DisciplineId,
            MembershipNumber = dto.MembershipNumber,
            JoinedAt = dto.JoinedAt == default ? DateOnly.FromDateTime(DateTime.UtcNow) : dto.JoinedAt,
            Status = "Active"
        };
        _db.ClubMemberships.Add(membership);
        _db.SaveChanges();
        return Created("/api/users/me/memberships", ResponseMapper.ToResponse(membership));
    }
}
