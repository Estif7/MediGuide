using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuide.API.Services;
using MediGuide.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<BookingHub> _hubContext;

    public DocumentsController(
        MediGuideDbContext context,
        IWebHostEnvironment env,
        IAuthorizationService authorizationService,
        INotificationService notificationService,
        IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _env = env;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    // GET all documents for a booking
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByBooking(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        var docs = await _context.Documents
            .AsNoTracking()
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

        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

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
        var uploadedBy = User.IsInRole("Agent") ? "Agent"
                    : User.IsInRole("Admin") ? "Admin"
                    : "Patient";

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

        await _hubContext.Clients.Group(BookingHub.GroupName(bookingId))
            .SendAsync("DocumentUploaded", result);

        var recipientId = uploadedBy == "Patient"
            ? (booking.AgentId.HasValue ? await _notificationService.GetUserIdForAgentAsync(booking.AgentId.Value) : null)
            : await _notificationService.GetUserIdForPatientAsync(booking.PatientId);

        if (recipientId is not null)
        {
            await _notificationService.NotifyAsync(
                recipientId, "DocumentUploaded", $"A new document was uploaded: {document.FileName}", bookingId);
        }

        return CreatedAtAction(nameof(GetByBooking), new { bookingId }, result);
    }

    // Optional: download
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc is null)
            return NotFound();

        var booking = await _context.Bookings.FindAsync(doc.BookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        var path = Path.Combine(_env.ContentRootPath, "Uploads", doc.StoragePath);
        if (!System.IO.File.Exists(path))
            return NotFound("File not found on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, doc.ContentType, doc.FileName);
    }
}
