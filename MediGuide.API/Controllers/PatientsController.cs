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

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAll()
    {
        var patients = await _context.Patients
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new PatientDto(
                p.Id,
                p.FullName,
                p.Email,
                p.PhoneNumber,
                p.PreferredLanguage,
                p.IsActive))
            .ToListAsync();

        return Ok(patients);
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
