using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Domain.Enums;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly MediGuideDbContext _context;

    public BookingsController(MediGuideDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Patient)
            .Include(b => b.ServiceCategory)
            .Include(b => b.Agent)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingDto(
                b.Id,
                b.PatientId,
                b.Patient.FullName,
                b.ServiceCategoryId,
                b.ServiceCategory.Name,
                b.AgentId,
                b.Agent != null ? b.Agent.FullName : null,
                b.ResponseTime,
                b.Status,
                b.Amount,
                b.Notes,
                b.CreatedAt))
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Patient)
            .Include(b => b.ServiceCategory)
            .Include(b => b.Agent)
            .Where(b => b.Id == id)
            .Select(b => new BookingDto(
                b.Id,
                b.PatientId,
                b.Patient.FullName,
                b.ServiceCategoryId,
                b.ServiceCategory.Name,
                b.AgentId,
                b.Agent != null ? b.Agent.FullName : null,
                b.ResponseTime,
                b.Status,
                b.Amount,
                b.Notes,
                b.CreatedAt))
            .FirstOrDefaultAsync();

        if (booking is null)
            return NotFound();

        return Ok(booking);
    }


    [Authorize] 
    // [Authorize(Roles = "Patient")]
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
    {
        // Validate patient exists
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient is null)
            return BadRequest("Patient not found.");

        // Validate category exists and is active
        var category = await _context.ServiceCategories.FindAsync(dto.ServiceCategoryId);
        if (category is null || !category.IsActive)
            return BadRequest("Service category not found or inactive.");

        var booking = new Booking
        {
            PatientId = dto.PatientId,
            ServiceCategoryId = dto.ServiceCategoryId,
            ResponseTime = dto.ResponseTime,
            Status = BookingStatus.PendingPayment,   // payment deferred for now
            Amount = category.BasePrice,             // snapshot the price
            Notes = dto.Notes
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // Reload with navigation properties for the response
        await _context.Entry(booking).Reference(b => b.Patient).LoadAsync();
        await _context.Entry(booking).Reference(b => b.ServiceCategory).LoadAsync();

        var result = new BookingDto(
            booking.Id,
            booking.PatientId,
            booking.Patient.FullName,
            booking.ServiceCategoryId,
            booking.ServiceCategory.Name,
            booking.AgentId,
            null,
            booking.ResponseTime,
            booking.Status,
            booking.Amount,
            booking.Notes,
            booking.CreatedAt);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, result);
    }

    // Optional: simple status update (useful later for assignment)
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] BookingStatus newStatus)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
            return NotFound();

        booking.Status = newStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/assign")]
    public async Task<ActionResult<BookingDto>> AssignAgent(Guid id, [FromBody] AssignAgentDto dto)
    {
        var booking = await _context.Bookings
            .Include(b => b.Patient)
            .Include(b => b.ServiceCategory)
            .Include(b => b.Agent)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound("Booking not found.");

        var agent = await _context.Agents.FindAsync(dto.AgentId);
        if (agent is null || !agent.IsActive)
            return BadRequest("Agent not found or inactive.");

        if (!agent.IsAvailable)
            return BadRequest("Agent is currently not available.");

        booking.AgentId = agent.Id;
        booking.Status = BookingStatus.Assigned;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload agent name for the response
        await _context.Entry(booking).Reference(b => b.Agent).LoadAsync();

        var result = new BookingDto(
            booking.Id,
            booking.PatientId,
            booking.Patient.FullName,
            booking.ServiceCategoryId,
            booking.ServiceCategory.Name,
            booking.AgentId,
            booking.Agent?.FullName,
            booking.ResponseTime,
            booking.Status,
            booking.Amount,
            booking.Notes,
            booking.CreatedAt);

        return Ok(result);
    }
}