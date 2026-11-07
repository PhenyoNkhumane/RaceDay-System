using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RegistrationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("~/api/events/{eventId:int}/registrations")]
    [RequireRole(Roles.Participant)]
    [EndpointSummary("Enrol the signed-in participant in an event")]
    [EndpointDescription("Participants only. Supply categoryId and optional temporary ASA licence and medical aid details. Returns 201 with the recorded participant/category enrolment; 400 for age eligibility issues, 404 for an unknown event or category, or 409 if the event is unavailable, the category is full, or the participant is already enrolled.")]
    [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, EnrolDto dto)
    {
        var ev = _db.Events.Find(eventId);
        var category = _db.Categories.Find(dto.CategoryId);
        if (ev is null || category is null || category.EventId != eventId)
        {
            return NotFound();
        }

        if (ev.Status != "Published" || ev.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Conflict("Registration is only available for future published events.");
        }

        var participantId = CurrentUser.GetUserId(HttpContext);
        if (participantId is null)
        {
            return Unauthorized();
        }

        if (category.CategoryType == "Age")
        {
            var idNumber = _db.Users.Where(u => u.Id == participantId).Select(u => u.IdNumberOrPassport).FirstOrDefault();
            var age = AgeCalculator.Calculate(idNumber, ev.Date);
            if (age is null)
            {
                return BadRequest("An age category requires an identity number containing a valid date of birth.");
            }

            if (age < category.MinAge || age > category.MaxAge)
            {
                return BadRequest("Participant does not meet this category's age requirements.");
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await _db.Registrations.AnyAsync(r =>
                r.ParticipantId == participantId && r.CategoryId == dto.CategoryId && r.Status == "Active"))
        {
            return Conflict("Already registered for this category.");
        }

        var activeCount = await _db.Registrations.CountAsync(r =>
            r.CategoryId == dto.CategoryId && r.Status == "Active");
        if (activeCount >= category.MaxSlots)
        {
            return Conflict("Category is full.");
        }

        var registration = new Registration
        {
            ParticipantId = participantId.Value,
            CategoryId = dto.CategoryId,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending",
            AmountPaid = 0,
            RequiresTempAsaLicense = dto.RequiresTempAsaLicense,
            MedicalAidName = dto.MedicalAidName,
            MedicalAidNumber = dto.MedicalAidNumber,
            Status = "Active"
        };
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Created($"/api/registrations/{registration.Id}", ResponseMapper.ToResponse(registration, includeMedicalAid: true));
    }

    [HttpGet("me")]
    [RequireRole(Roles.Participant)]
    [EndpointSummary("List the signed-in participant's enrolments")]
    [EndpointDescription("Returns the current Participant's event category enrolments and their payment and registration status. Participants only; requires an authenticated session.")]
    [ProducesResponseType(typeof(List<RegistrationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetMine()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(_db.Registrations.Where(r => r.ParticipantId == userId)
            .Select(r => new RegistrationResponseDto
            {
                Id = r.Id,
                CategoryId = r.CategoryId,
                ParticipantId = r.ParticipantId,
                RegistrationDate = r.RegistrationDate,
                PaymentStatus = r.PaymentStatus,
                AmountPaid = r.AmountPaid,
                AssignedBibNumber = r.AssignedBibNumber,
                RequiresTempAsaLicense = r.RequiresTempAsaLicense,
                MedicalAidName = r.MedicalAidName,
                MedicalAidNumber = r.MedicalAidNumber,
                Status = r.Status
            }).ToList());
    }

    [HttpGet("~/api/events/{eventId:int}/registrations")]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("List enrolments for an organiser's event")]
    [EndpointDescription("Returns all enrolments, including selected categories and registration details, for the specified event. Only its owning Organiser may access this list; returns 404 if missing or 403 if owned by another organiser.")]
    [ProducesResponseType(typeof(List<RegistrationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetForEvent(int eventId)
    {
        var ev = _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var registrations = _db.Registrations
            .Include(r => r.Category)
            .Where(r => r.Category!.EventId == eventId)
            .AsEnumerable()
            .Select(r => ResponseMapper.ToResponse(r))
            .ToList();
        return Ok(registrations);
    }

    [HttpGet("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    [EndpointSummary("Get an enrolment")]
    [EndpointDescription("Returns an enrolment for its Participant or the Organiser who owns its event. Medical aid details are returned only to the Participant. Returns 404 if missing or 403 if the signed-in user does not own it.")]
    [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetRegistration(int registrationId)
    {
        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(ResponseMapper.ToResponse(registration,
            includeMedicalAid: CurrentUser.GetRole(HttpContext) == Roles.Participant));
    }

    [HttpPut("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    [EndpointSummary("Update an enrolment")]
    [EndpointDescription("The owning Participant may update medical aid details; the owning event Organiser may update payment status, amount paid, bib number and registration status. Returns the updated enrolment, 404 if missing, or 403 if the user does not own it.")]
    [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateRegistration(int registrationId, UpdateRegistrationDto dto)
    {
        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var role = CurrentUser.GetRole(HttpContext);
        if (role == Roles.Participant)
        {
            registration.MedicalAidName = dto.MedicalAidName ?? registration.MedicalAidName;
            registration.MedicalAidNumber = dto.MedicalAidNumber ?? registration.MedicalAidNumber;
        }
        else
        {
            if (dto.PaymentStatus is not null &&
                dto.PaymentStatus is not "Pending" and not "Paid" and not "Refunded")
            {
                return BadRequest("Invalid payment status.");
            }

            if (dto.Status is not null && dto.Status is not "Active" and not "Cancelled")
            {
                return BadRequest("Invalid registration status.");
            }

            if (dto.AmountPaid is < 0)
            {
                return BadRequest("Amount paid cannot be negative.");
            }

            registration.PaymentStatus = dto.PaymentStatus ?? registration.PaymentStatus;
            registration.AmountPaid = dto.AmountPaid ?? registration.AmountPaid;
            registration.AssignedBibNumber = dto.AssignedBibNumber ?? registration.AssignedBibNumber;
            registration.Status = dto.Status ?? registration.Status;
        }

        _db.SaveChanges();
        return Ok(ResponseMapper.ToResponse(registration,
            includeMedicalAid: CurrentUser.GetRole(HttpContext) == Roles.Participant));
    }

    [HttpDelete("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    [EndpointSummary("Cancel an enrolment")]
    [EndpointDescription("The owning Participant or event Organiser may cancel an enrolment that has no result. Returns 204 on success, 404 if missing, 403 if the user does not own it, or 409 if a result is already recorded.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CancelRegistration(int registrationId)
    {
        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (_db.Results.Any(result => result.RegistrationId == registrationId))
        {
            return Conflict("A registration with a recorded result cannot be cancelled.");
        }

        registration.Status = "Cancelled";
        _db.SaveChanges();
        return NoContent();
    }

    private bool CanAccess(Registration registration)
    {
        var userId = CurrentUser.GetUserId(HttpContext)!.Value;
        var role = CurrentUser.GetRole(HttpContext);
        if (role == Roles.Participant)
        {
            return registration.ParticipantId == userId;
        }

        var eventId = registration.Category?.EventId;
        var ev = eventId is null ? null : _db.Events.Find(eventId);
        return ev is not null && ev.OrganiserId == userId;
    }
}
