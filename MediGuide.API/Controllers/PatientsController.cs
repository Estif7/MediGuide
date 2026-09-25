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
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(new PagedResult<PatientDto>(patients, totalCount, page, pageSize));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PatientDto>> GetCurrentPatient()
    {
        if (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId))
            return Forbid();

        var patient = await _context.Patients.FindAsync(patientId);
        if (patient is null || !patient.IsActive)
            return NotFound();

        return Ok(ToDto(patient));
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<ActionResult<PatientDto>> UpdateCurrentPatient(UpdatePatientProfileDto dto)
    {
        if (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId))
            return Forbid();

        var patient = await _context.Patients.FindAsync(patientId);
        if (patient is null || !patient.IsActive)
            return NotFound();

        patient.FullName = dto.FullName;
        patient.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(dto.PreferredLanguage))
            patient.PreferredLanguage = dto.PreferredLanguage;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender;
        patient.EmergencyContactName = dto.EmergencyContactName;
        patient.EmergencyContactPhone = dto.EmergencyContactPhone;
        patient.Allergies = dto.Allergies;
        patient.ChronicConditions = dto.ChronicConditions;
        patient.CurrentMedications = dto.CurrentMedications;
        patient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToDto(patient));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Agent")
            && (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId) || patientId != id))
        {
            return Forbid();
        }

        var patient = await _context.Patients.FindAsync(id);
        if (patient is null)
            return NotFound();

        return Ok(ToDto(patient));
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
            PreferredLanguage = dto.PreferredLanguage ?? "en",
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            Allergies = dto.Allergies,
            ChronicConditions = dto.ChronicConditions,
            CurrentMedications = dto.CurrentMedications
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, ToDto(patient));
    }

    private static PatientDto ToDto(Patient p) => new(
        p.Id,
        p.FullName,
        p.Email,
        p.PhoneNumber,
        p.PreferredLanguage,
        p.IsActive,
        p.DateOfBirth,
        p.Gender,
        p.EmergencyContactName,
        p.EmergencyContactPhone,
        p.Allergies,
        p.ChronicConditions,
        p.CurrentMedications);
}
