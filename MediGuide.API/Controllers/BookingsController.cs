using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Domain.Enums;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MediGuide.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHubContext<BookingHub> _hubContext;

    public BookingsController(
        MediGuideDbContext context,
        IAuthorizationService authorizationService,
        IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _authorizationService = authorizationService;
        _hubContext = hubContext;
    }
 
    private const int MaxPageSize = 100;

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetAll([FromQuery] BookingQueryParams query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var bookingsQuery = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.ServiceCategory)
            .Include(b => b.Agent)
            .AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            if (Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId))
                bookingsQuery = bookingsQuery.Where(b => b.PatientId == patientId);
            else if (Guid.TryParse(User.FindFirst("agentId")?.Value, out var agentId))
                bookingsQuery = bookingsQuery.Where(b => b.AgentId == agentId);
            else
                return Forbid();
        }

        if (query.Status.HasValue)
            bookingsQuery = bookingsQuery.Where(b => b.Status == query.Status.Value);

        // Name search only meaningful for Admin (patients/agents are already scoped to themselves)
        if (User.IsInRole("Admin"))
        {
            if (!string.IsNullOrWhiteSpace(query.PatientName))
                bookingsQuery = bookingsQuery.Where(b => EF.Functions.ILike(b.Patient.FullName, $"%{query.PatientName}%"));

            if (!string.IsNullOrWhiteSpace(query.AgentName))
                bookingsQuery = bookingsQuery.Where(b => b.Agent != null && EF.Functions.ILike(b.Agent.FullName, $"%{query.AgentName}%"));
        }

        bookingsQuery = (query.SortBy, query.SortDir.ToLowerInvariant()) switch
        {
            ("Status", "asc") => bookingsQuery.OrderBy(b => b.Status),
            ("Status", _) => bookingsQuery.OrderByDescending(b => b.Status),
            (_, "asc") => bookingsQuery.OrderBy(b => b.CreatedAt),
            _ => bookingsQuery.OrderByDescending(b => b.CreatedAt),
        };

        var totalCount = await bookingsQuery.CountAsync();

        var bookings = await bookingsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return Ok(new PagedResult<BookingDto>(bookings, totalCount, page, pageSize));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid id)
    {
        var booking = await LoadBooking(id);

        if (booking is null)
            return NotFound();

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(ToDto(booking));
    }


    [Authorize(Roles = "Patient,Admin")]
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
    {
        // Validate patient exists
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient is null)
            return BadRequest("Patient not found.");

        if (!User.IsInRole("Admin")
            && (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId) || patientId != dto.PatientId))
        {
            return Forbid();
        }

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

    

    [Authorize(Roles = "Admin")]
[HttpPatch("{id:guid}/assign")]
public async Task<ActionResult<BookingDto>> AssignAgent(Guid id, [FromBody] AssignAgentDto dto)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    if (booking.Status != BookingStatus.Paid)
        return BadRequest("Booking must be paid before an agent can be assigned.");

    var agent = await _context.Agents.FindAsync(dto.AgentId);
    if (agent is null || !agent.IsActive)
        return BadRequest("Agent not found or inactive.");

    booking.AgentId = agent.Id;
    booking.Status = BookingStatus.Assigned;
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _context.Entry(booking).Reference(b => b.Agent).LoadAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    return Ok(ToDto(booking));
}

[Authorize(Roles = "Agent")]
[HttpPatch("{id:guid}/accept")]
public async Task<ActionResult<BookingDto>> Accept(Guid id)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    var agentId = GetCurrentAgentId();
    if (agentId is null || booking.AgentId != agentId)
        return Forbid();

    if (booking.Status != BookingStatus.Assigned)
        return BadRequest("Only assigned bookings can be accepted.");

    booking.Status = BookingStatus.InProgress;
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    return Ok(ToDto(booking));
}

[Authorize(Roles = "Agent")]
[HttpPatch("{id:guid}/decline")]
public async Task<ActionResult<BookingDto>> Decline(Guid id)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    var agentId = GetCurrentAgentId();
    if (agentId is null || booking.AgentId != agentId)
        return Forbid();

    if (booking.Status != BookingStatus.Assigned)
        return BadRequest("Only assigned bookings can be declined.");

    booking.AgentId = null;
    booking.Status = BookingStatus.Paid; // back to admin pool
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    return Ok(ToDto(booking));
}

[Authorize(Roles = "Agent")]
[HttpPatch("{id:guid}/refer")]
public async Task<ActionResult<BookingDto>> Refer(Guid id, [FromBody] AssignAgentDto dto)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    var agentId = GetCurrentAgentId();
    if (agentId is null || booking.AgentId != agentId)
        return Forbid();

    if (booking.Status != BookingStatus.Assigned && booking.Status != BookingStatus.InProgress)
        return BadRequest("Booking cannot be referred in its current status.");

    if (dto.AgentId == agentId)
        return BadRequest("Cannot refer to yourself.");

    var other = await _context.Agents.FindAsync(dto.AgentId);
    if (other is null || !other.IsActive)
        return BadRequest("Target agent not found or inactive.");

    booking.AgentId = other.Id;
    booking.Status = BookingStatus.Assigned;
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _context.Entry(booking).Reference(b => b.Agent).LoadAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
    .SendAsync("BookingUpdated", ToDto(booking));

    return Ok(ToDto(booking));
}

private async Task<Booking?> LoadBooking(Guid id) =>
    await _context.Bookings
        .Include(b => b.Patient)
        .Include(b => b.ServiceCategory)
        .Include(b => b.Agent)
        .FirstOrDefaultAsync(b => b.Id == id);

private Guid? GetCurrentAgentId()
{
    var claim = User.FindFirst("agentId")?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
}

private static BookingDto ToDto(Booking b) => new(
    b.Id,
    b.PatientId,
    b.Patient.FullName,
    b.ServiceCategoryId,
    b.ServiceCategory.Name,
    b.AgentId,
    b.Agent?.FullName,
    b.ResponseTime,
    b.Status,
    b.Amount,
    b.Notes,
    b.CreatedAt);

private static readonly BookingStatus[] AdminAssignableStatuses =
{
    BookingStatus.PendingPayment,
    BookingStatus.Paid,
    BookingStatus.Assigned,
    BookingStatus.InProgress
};

[Authorize(Roles = "Admin")]
[HttpPatch("{id:guid}/booking-status")]
public async Task<ActionResult<BookingDto>> UpdateBookingStatus(Guid id, UpdateBookingStatusDto dto)
{
    if (!AdminAssignableStatuses.Contains(dto.Status))
        return BadRequest($"Status must be one of: {string.Join(", ", AdminAssignableStatuses)}");

    var booking = await LoadBooking(id);
    if (booking is null)
        return NotFound();

    booking.Status = dto.Status;
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
    .SendAsync("BookingUpdated", ToDto(booking));
    
    return Ok(ToDto(booking));
}
}