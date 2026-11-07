using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DisciplinesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [EndpointSummary("List event disciplines")]
    [EndpointDescription("Returns available disciplines with their names and descriptions. No request body is required.")]
    [ProducesResponseType(typeof(List<DisciplineResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetDisciplines()
    {
        return Ok(_db.Disciplines.Select(discipline => new DisciplineResponseDto
        {
            Id = discipline.Id,
            Name = discipline.Name,
            Description = discipline.Description
        }).ToList());
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    [EndpointSummary("Create a discipline")]
    [EndpointDescription("Organisers only. Supply a name and optional description. Returns 201 with the created discipline, or 409 if the name is already in use.")]
    [ProducesResponseType(typeof(DisciplineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CreateDiscipline(CreateDisciplineDto dto)
    {
        if (_db.Disciplines.Any(d => d.Name == dto.Name))
        {
            return Conflict("A discipline with this name already exists.");
        }

        var discipline = new Discipline
        {
            Name = dto.Name,
            Description = dto.Description
        };
        _db.Disciplines.Add(discipline);
        _db.SaveChanges();
        return Created($"/api/disciplines/{discipline.Id}", ResponseMapper.ToResponse(discipline));
    }
}
