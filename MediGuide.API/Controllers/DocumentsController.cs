using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(MediGuideDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET all documents for a booking
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByBooking(Guid bookingId)
    {
        var docs = await _context.Documents
            .Where(d => d.BookingId == bookingId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentDto(
                d.Id,
                d.BookingId,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedBy,
                d.CreatedAt))
            .ToListAsync();

        return Ok(docs);
    }

    // Upload a file to a booking
    [HttpPost("booking/{bookingId:guid}")]
    public async Task<ActionResult<DocumentDto>> Upload(Guid bookingId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var bookingExists = await _context.Bookings.AnyAsync(b => b.Id == bookingId);
        if (!bookingExists)
            return NotFound("Booking not found.");

        // Simple local storage (good enough for now)
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Who is uploading?
        var uploadedBy = User.IsInRole("Agent") ? "Agent" : "Patient";

        var document = new Document
        {
            BookingId = bookingId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            StoragePath = uniqueName,          // relative name
            FileSizeBytes = file.Length,
            UploadedBy = uploadedBy
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var result = new DocumentDto(
            document.Id,
            document.BookingId,
            document.FileName,
            document.ContentType,
            document.FileSizeBytes,
            document.UploadedBy,
            document.CreatedAt);

        return CreatedAtAction(nameof(GetByBooking), new { bookingId }, result);
    }

    // Optional: download
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc is null)
            return NotFound();

        var path = Path.Combine(_env.ContentRootPath, "Uploads", doc.StoragePath);
        if (!System.IO.File.Exists(path))
            return NotFound("File not found on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, doc.ContentType, doc.FileName);
    }
}