using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MediGuideDbContext _context;

    public PatientsController(MediGuideDbContext context)
    {
        _context = context;
    }

    private const int MaxPageSize = 100;

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> GetAll([FromQuery] PatientQueryParams query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var patientsQuery = _context.Patients.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Name))
            patientsQuery = patientsQuery.Where(p => EF.Functions.ILike(p.FullName, $"%{query.Name}%"));

        var totalCount = await patientsQuery.CountAsync();

        var patients = await patientsQuery
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientDto(
                p.Id,
                p.FullName,
                p.Email,
                p.PhoneNumber,
                p.PreferredLanguage,
                p.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<PatientDto>(patients, totalCount, page, pageSize));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        if (!User.IsInRole("Admin")
            && (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId) || patientId != id))
        {
            return Forbid();
        }

        var patient = await _context.Patients
            .Where(p => p.Id == id)
            .Select(p => new PatientDto(
                p.Id,
                p.FullName,
                p.Email,
                p.PhoneNumber,
                p.PreferredLanguage,
                p.IsActive))
            .FirstOrDefaultAsync();

        if (patient is null)
            return NotFound();

        return Ok(patient);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientDto dto)
    {
        var patient = new Patient
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PreferredLanguage = dto.PreferredLanguage ?? "en"
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var result = new PatientDto(
            patient.Id,
            patient.FullName,
            patient.Email,
            patient.PhoneNumber,
            patient.PreferredLanguage,
            patient.IsActive);

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, result);
    }
}
