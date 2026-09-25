using System.Security.Claims;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestimonialsController : ControllerBase
{
    private readonly MediGuideDbContext _context;

    public TestimonialsController(MediGuideDbContext context)
    {
        _context = context;
    }

    // Public list of approved testimonials for landing page
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetApproved()
    {
        var list = await _context.Testimonials
            .AsNoTracking()
            .Where(t => t.IsApproved)
            .Include(t => t.Patient)
            .OrderByDescending(t => t.CreatedAt)
            .Take(12)
            .Select(t => new TestimonialDto(
                t.Id,
                t.Patient.FullName,
                t.Rating,
                t.Comment,
                t.CreatedAt))
            .ToListAsync();

        return Ok(list);
    }

    // Admin moderation queue
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminTestimonialDto>>> GetAllForAdmin()
    {
        var list = await _context.Testimonials
            .AsNoTracking()
            .Include(t => t.Patient)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AdminTestimonialDto(
                t.Id,
                t.PatientId,
                t.Patient.FullName,
                t.Patient.Email,
                t.Rating,
                t.Comment,
                t.IsApproved,
                t.BookingId,
                t.CreatedAt))
            .ToListAsync();

        return Ok(list);
    }

    // Patient submits a testimonial
    [Authorize(Roles = "Patient")]
    [HttpPost]
    public async Task<ActionResult<TestimonialDto>> Submit(CreateTestimonialDto dto)
    {
        if (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId))
            return Forbid();

        var patient = await _context.Patients.FindAsync(patientId);
        if (patient is null || !patient.IsActive)
            return BadRequest("Patient not found.");

        if (dto.BookingId.HasValue)
        {
            var booking = await _context.Bookings.FindAsync(dto.BookingId.Value);
            if (booking is null || booking.PatientId != patientId)
                return BadRequest("Invalid booking associated with this testimonial.");
        }

        var testimonial = new Testimonial
        {
            PatientId = patientId,
            BookingId = dto.BookingId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Comment = dto.Comment.Trim(),
            IsApproved = false // requires admin approval before public listing
        };

        _context.Testimonials.Add(testimonial);
        await _context.SaveChangesAsync();

        var result = new TestimonialDto(
            testimonial.Id,
            patient.FullName,
            testimonial.Rating,
            testimonial.Comment,
            testimonial.CreatedAt);

        return Ok(result);
    }

    // Admin approves testimonial
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var item = await _context.Testimonials.FindAsync(id);
        if (item is null)
            return NotFound();

        item.IsApproved = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Admin deletes/rejects testimonial
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.Testimonials.FindAsync(id);
        if (item is null)
            return NotFound();

        _context.Testimonials.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
