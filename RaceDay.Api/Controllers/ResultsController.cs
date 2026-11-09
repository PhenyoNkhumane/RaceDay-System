using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private static readonly string[] ResultStatuses = ["Finished", "DNF", "DNS", "DQ"];

    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("~/api/events/{eventId:int}/results")]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("Record a participant's event result")]
    [EndpointDescription("The event's Organiser supplies registrationId, optional gunTime and netTime, optional overallPosition and categoryPosition, and status (Finished, DNF, DNS or DQ). Returns 201 with the recorded result; 403 if not the event owner, or 409 if the registration already has a result.")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CreateResult(int eventId, CreateResultDto dto)
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

        var registration = _db.Registrations.Include(r => r.Category)
            .FirstOrDefault(r => r.Id == dto.RegistrationId);
        if (registration is null || registration.Category?.EventId != eventId)
        {
            return NotFound();
        }

        if (registration.Status != "Active")
        {
            return Conflict("Results can only be recorded for active registrations.");
        }

        if (_db.Results.Any(r => r.RegistrationId == dto.RegistrationId))
        {
            return Conflict("A result already exists for this registration.");
        }

        if (!ResultStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid result status.");
        }

        if (dto.OverallPosition is <= 0 || dto.CategoryPosition is <= 0)
        {
            return BadRequest("Positions must be greater than zero.");
        }

        var validation = ValidateResultValues(dto.GunTime, dto.NetTime, dto.OverallPosition,
            dto.CategoryPosition, eventId, registration.CategoryId);
        if (validation is not null)
        {
            return validation;
        }

        var result = new Result
        {
            RegistrationId = dto.RegistrationId,
            GunTime = dto.GunTime,
            NetTime = dto.NetTime,
            OverallPosition = dto.OverallPosition,
            CategoryPosition = dto.CategoryPosition,
            Status = dto.Status
        };
        _db.Results.Add(result);
        _db.SaveChanges();
        return Created($"/api/results/{result.Id}", ResponseMapper.ToResponse(result));
    }

    [HttpPut("{resultId:int}")]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("Update an event result")]
    [EndpointDescription("The owning event Organiser may update gunTime, netTime, overallPosition, categoryPosition or status. Returns the updated result, 404 if missing, or 403 if the result belongs to another organiser's event.")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateResult(int resultId, UpdateResultDto dto)
    {
        var result = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .FirstOrDefault(r => r.Id == resultId);
        if (result is null)
        {
            return NotFound();
        }

        var eventId = result.Registration?.Category?.EventId;
        var ev = eventId is null ? null : _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (dto.Status is not null && !ResultStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid result status.");
        }

        if (dto.OverallPosition is <= 0 || dto.CategoryPosition is <= 0)
        {
            return BadRequest("Positions must be greater than zero.");
        }

        var validation = ValidateResultValues(
            dto.GunTime ?? result.GunTime,
            dto.NetTime ?? result.NetTime,
            dto.OverallPosition ?? result.OverallPosition,
            dto.CategoryPosition ?? result.CategoryPosition,
            ev.Id,
            result.Registration!.CategoryId,
            result.Id);
        if (validation is not null)
        {
            return validation;
        }

        result.GunTime = dto.GunTime ?? result.GunTime;
        result.NetTime = dto.NetTime ?? result.NetTime;
        result.OverallPosition = dto.OverallPosition ?? result.OverallPosition;
        result.CategoryPosition = dto.CategoryPosition ?? result.CategoryPosition;
        result.Status = dto.Status ?? result.Status;
        _db.SaveChanges();
        return Ok(ResponseMapper.ToResponse(result));
    }

    [HttpGet("~/api/events/{eventId:int}/results")]
    [EndpointSummary("List results for a published event")]
    [EndpointDescription("Returns the published event's participant results, including category, finish times, positions and result status. Returns 404 if the event is missing or not published.")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetForEvent(int eventId)
    {
        if (!_db.Events.Any(e => e.Id == eventId && e.Status == "Published"))
        {
            return NotFound();
        }

        var results = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .ThenInclude(c => c!.Event)
            .Where(r => r.Registration!.Category!.EventId == eventId &&
                r.Registration.Status == "Active")
            .AsEnumerable().Select(ResponseMapper.ToResponse).ToList();
        var finisherCount = results.Count(r => r.Status == "Finished");
        results = results.Select(r => new ResultResponseDto
        {
            Id = r.Id, CategoryId = r.CategoryId, GunTime = r.GunTime, NetTime = r.NetTime,
            OverallPosition = r.OverallPosition, CategoryPosition = r.CategoryPosition, Status = r.Status,
            EventName = r.EventName, EventDate = r.EventDate, CategoryName = r.CategoryName,
            FinishTime = r.FinishTime, FinisherCount = finisherCount
        }).ToList();
        return Ok(results);
    }

    [HttpGet("~/api/events/{eventId:int}/results/manage")]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("List results for the owning organiser")]
    [EndpointDescription("Organisers only. Returns the results and associated registration IDs for the specified event when it belongs to the signed-in Organiser.")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetForOrganiser(int eventId)
    {
        var ev = _db.Events.Find(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext)) return Forbid();

        var results = _db.Results.Include(r => r.Registration).ThenInclude(r => r!.Category)
            .ThenInclude(c => c!.Event)
            .Where(r => r.Registration!.Category!.EventId == eventId)
            .AsEnumerable().Select(ResponseMapper.ToResponse).ToList();
        var finishers = results.Count(r => r.Status == "Finished");
        return Ok(results.Select(r => new ResultResponseDto
        {
            Id = r.Id, RegistrationId = r.RegistrationId, CategoryId = r.CategoryId,
            GunTime = r.GunTime, NetTime = r.NetTime, OverallPosition = r.OverallPosition,
            CategoryPosition = r.CategoryPosition, Status = r.Status, EventName = r.EventName,
            EventDate = r.EventDate, CategoryName = r.CategoryName, FinishTime = r.FinishTime,
            FinisherCount = finishers
        }).ToList());
    }

    [HttpGet("me")]
    [RequireRole(Roles.Participant)]
    [EndpointSummary("List the signed-in participant's results")]
    [EndpointDescription("Returns results for the current Participant's own event enrolments, including finish times, positions and status. Participants only; requires an authenticated session.")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetMine()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var results = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .ThenInclude(c => c!.Event)
            .Where(r => r.Registration!.ParticipantId == userId)
            .AsEnumerable().Select(ResponseMapper.ToResponse).ToList();
        var finishersByEvent = _db.Results.Where(r => r.Status == "Finished")
            .Select(r => r.Registration!.Category!.EventId).AsEnumerable()
            .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        results = results.Select(r => new ResultResponseDto
        {
            Id = r.Id, RegistrationId = r.RegistrationId, CategoryId = r.CategoryId, GunTime = r.GunTime, NetTime = r.NetTime,
            OverallPosition = r.OverallPosition, CategoryPosition = r.CategoryPosition, Status = r.Status,
            EventName = r.EventName, EventDate = r.EventDate, CategoryName = r.CategoryName,
            FinishTime = r.FinishTime,
            FinisherCount = r.RegistrationEventId is int id && finishersByEvent.TryGetValue(id, out var count) ? count : null
        }).ToList();
        return Ok(results);
    }

    [HttpGet("{resultId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    [EndpointSummary("Get an event result")]
    [EndpointDescription("Returns a result to its Participant or the Organiser who owns its event. Returns 404 if missing or 403 if the signed-in user is not authorized to view it.")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetResult(int resultId)
    {
        var result = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .ThenInclude(c => c!.Event)
            .FirstOrDefault(r => r.Id == resultId);
        if (result is null)
        {
            return NotFound();
        }

        var userId = CurrentUser.GetUserId(HttpContext)!.Value;
        var role = CurrentUser.GetRole(HttpContext);
        if (role == Roles.Participant && result.Registration?.ParticipantId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (role == Roles.Organiser)
        {
            var eventId = result.Registration?.Category?.EventId;
            var ev = eventId is null ? null : _db.Events.Find(eventId);
            if (ev is null || ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        return Ok(ResponseMapper.ToResponse(result));
    }

    private IActionResult? ValidateResultValues(TimeOnly? gunTime, TimeOnly? netTime,
        int? overallPosition, int? categoryPosition, int eventId, int categoryId, int? resultId = null)
    {
        if (gunTime is not null && netTime is not null && netTime > gunTime)
        {
            return BadRequest("Net time cannot be greater than gun time.");
        }

        if (overallPosition is not null && _db.Results.Any(r =>
                r.Id != resultId &&
                r.OverallPosition == overallPosition &&
                r.Registration!.Category!.EventId == eventId))
        {
            return Conflict("Overall position is already assigned for this event.");
        }

        if (categoryPosition is not null && _db.Results.Any(r =>
                r.Id != resultId &&
                r.CategoryPosition == categoryPosition &&
                r.Registration!.CategoryId == categoryId))
        {
            return Conflict("Category position is already assigned.");
        }

        return null;
    }
}
