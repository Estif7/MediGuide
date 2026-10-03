using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Domain.Enums;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MediGuide.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using MediGuide.API.Services;
using Microsoft.AspNetCore.Identity;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHubContext<BookingHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingsController(
        MediGuideDbContext context,
        IAuthorizationService authorizationService,
        IHubContext<BookingHub> hubContext,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _authorizationService = authorizationService;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _userManager = userManager;
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
            .Include(b => b.ReferredToAgent)
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
                b.CreatedAt,
                b.IsReferralPendingApproval,
                b.ReferredToAgentId,
                b.ReferredToAgent != null ? b.ReferredToAgent.FullName : null,
                b.ReferralReason,
                b.ReferralClinicalNotes))
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

        decimal multiplier = dto.ResponseTime switch
        {
            ResponseTime.Priority => 1.75m,
            ResponseTime.Expedited => 1.30m,
            _ => 1.0m
        };
        var finalAmount = Math.Round(category.BasePrice * multiplier, 2);

        var booking = new Booking
        {
            PatientId = dto.PatientId,
            ServiceCategoryId = dto.ServiceCategoryId,
            ResponseTime = dto.ResponseTime,
            Status = BookingStatus.PendingPayment,
            Amount = finalAmount,
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
            booking.Agent?.FullName,
            booking.ResponseTime,
            booking.Status,
            booking.Amount,
            booking.Notes,
            booking.CreatedAt,
            booking.IsReferralPendingApproval,
            booking.ReferredToAgentId,
            null,
            booking.ReferralReason,
            booking.ReferralClinicalNotes);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, result);
    }

    

    [Authorize(Roles = "Patient,Admin")]
    [HttpPost("{id:guid}/simulate-payment")]
    public async Task<ActionResult<BookingDto>> SimulatePayment(Guid id, [FromBody] SimulatePaymentDto? dto)
    {
        var booking = await LoadBooking(id);
        if (booking is null) return NotFound();

        if (!User.IsInRole("Admin"))
        {
            if (!Guid.TryParse(User.FindFirst("patientId")?.Value, out var patientId) || booking.PatientId != patientId)
                return Forbid();
        }

        if (booking.Status != BookingStatus.PendingPayment)
            return BadRequest("Only bookings pending payment can be paid.");

        var paymentMethod = string.IsNullOrWhiteSpace(dto?.PaymentMethod) ? "Telebirr" : dto.PaymentMethod;

        booking.Status = BookingStatus.Paid;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("BookingUpdated", ToDto(booking));

        var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
        if (patientUserId is not null)
        {
            await _notificationService.NotifyAsync(
                patientUserId, "PaymentConfirmed",
                $"Payment of {booking.Amount:N2} ETB confirmed via {paymentMethod}. Your consultation request has been queued for specialist assignment.", booking.Id);
        }

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            await _notificationService.NotifyAsync(
                admin.Id, "BookingPaid",
                $"Booking for {booking.Patient.FullName} ({booking.ServiceCategory.Name}) has been paid via {paymentMethod} ({booking.Amount:N2} ETB). Ready for agent assignment.", booking.Id);
        }

        return Ok(ToDto(booking));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/assign")]
    public async Task<ActionResult<BookingDto>> AssignAgent(Guid id, [FromBody] AssignAgentDto dto)
    {
        var booking = await LoadBooking(id);
        if (booking is null) return NotFound();

        if (booking.Status == BookingStatus.PendingPayment)
            return BadRequest("Booking must be paid before an agent can be assigned. Please complete payment first.");

        if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            return BadRequest("Cannot assign an agent to a completed or cancelled booking.");

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

    var agentUserId = await _notificationService.GetUserIdForAgentAsync(agent.Id);
    if (agentUserId is not null)
    {
        await _notificationService.NotifyAsync(
            agentUserId, "BookingAssigned",
            $"You've been assigned a new booking ({booking.ServiceCategory.Name}).", booking.Id);
    }

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

    if (booking.IsReferralPendingApproval)
        return BadRequest("Cannot accept consultation while a referral approval is pending.");

    booking.Status = BookingStatus.InProgress;
    booking.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
    if (patientUserId is not null)
    {
        await _notificationService.NotifyAsync(
            patientUserId, "BookingStatusChanged",
            "Your booking is now in progress.", booking.Id);
    }

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

        var decliningAgentName = booking.Agent?.FullName ?? "Healthcare Professional";

        // Return booking to Paid status so Administrator can manually reassign
        // based on knowledge of department, specialty, and patient needs
        booking.AgentId = null;
        booking.Status = BookingStatus.Paid;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("BookingUpdated", ToDto(booking));

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            await _notificationService.NotifyAsync(
                admin.Id, "BookingDeclined",
                $"{decliningAgentName} declined booking #{booking.Id.ToString()[..8]} ({booking.ServiceCategory.Name}). Manual assignment required based on specialty needs.", booking.Id);
        }

        return Ok(ToDto(booking));
    }

[Authorize(Roles = "Agent,Admin")]
[HttpPatch("{id:guid}/refer")]
public async Task<ActionResult<BookingDto>> Refer(Guid id, [FromBody] ReferBookingDto dto)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    Guid referringAgentId;
    string referringAgentName;

    if (User.IsInRole("Admin"))
    {
        referringAgentId = booking.AgentId ?? Guid.Empty;
        referringAgentName = booking.Agent?.FullName ?? "Admin";
    }
    else
    {
        var agentId = GetCurrentAgentId();
        if (agentId is null || booking.AgentId != agentId)
            return Forbid();
        referringAgentId = agentId.Value;
        referringAgentName = booking.Agent?.FullName ?? "Specialist";
    }

    if (booking.Status != BookingStatus.Assigned && booking.Status != BookingStatus.InProgress)
        return BadRequest("Booking cannot be referred in its current status.");

    if (booking.IsReferralPendingApproval)
        return BadRequest("A referral request is already pending admin review for this booking.");

    if (dto.TargetAgentId == booking.AgentId)
        return BadRequest("Cannot refer to the same assigned specialist.");

    var other = await _context.Agents.FindAsync(dto.TargetAgentId);
    if (other is null || !other.IsActive)
        return BadRequest("Target specialist agent not found or inactive.");

    if (User.IsInRole("Admin"))
    {
        // Admin direct reassignment / transfer without pending approval
        var handoverContent = $"[Clinical Referral Direct Transfer by Admin]\n" +
                              $"Transferred to: {other.FullName} ({other.Department})\n" +
                              $"Reason: {dto.Reason.Trim()}\n" +
                              (string.IsNullOrWhiteSpace(dto.ClinicalNotes) ? "" : $"Clinical Notes: {dto.ClinicalNotes.Trim()}");

        var handoverNote = new InternalNote
        {
            BookingId = booking.Id,
            AgentId = other.Id,
            Content = handoverContent.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.InternalNotes.Add(handoverNote);

        booking.AgentId = other.Id;
        booking.Status = BookingStatus.Assigned;
        booking.IsReferralPendingApproval = false;
        booking.ReferredToAgentId = null;
        booking.ReferralReason = null;
        booking.ReferralClinicalNotes = null;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _context.Entry(booking).Reference(b => b.Agent).LoadAsync();

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("BookingUpdated", ToDto(booking));

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("InternalNoteAdded", new InternalNoteDto(
                handoverNote.Id,
                handoverNote.BookingId,
                handoverNote.AgentId,
                "Admin",
                handoverNote.Content,
                handoverNote.CreatedAt));

        var referredUserId = await _notificationService.GetUserIdForAgentAsync(other.Id);
        if (referredUserId is not null)
        {
            await _notificationService.NotifyAsync(
                referredUserId, "BookingReferred",
                $"A consultation ({booking.ServiceCategory.Name}) has been transferred to you by Admin. Reason: {dto.Reason}.", booking.Id);
        }

        var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
        if (patientUserId is not null)
        {
            await _notificationService.NotifyAsync(
                patientUserId, "BookingReferred",
                $"Your consultation has been transferred to specialist {other.FullName} for further care.", booking.Id);
        }

        return Ok(ToDto(booking));
    }
    else
    {
        // Agent clinical referral initiation: Keeps current agent, flags pending admin approval
        var handoverContent = $"[Clinical Referral Requested - Awaiting Admin Approval]\n" +
                              $"Referred by: {referringAgentName} -> {other.FullName} ({other.Department})\n" +
                              $"Reason: {dto.Reason.Trim()}\n" +
                              (string.IsNullOrWhiteSpace(dto.ClinicalNotes) ? "" : $"Clinical Notes: {dto.ClinicalNotes.Trim()}");

        var handoverNote = new InternalNote
        {
            BookingId = booking.Id,
            AgentId = referringAgentId,
            Content = handoverContent.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.InternalNotes.Add(handoverNote);

        booking.IsReferralPendingApproval = true;
        booking.ReferredToAgentId = other.Id;
        booking.ReferralReason = dto.Reason.Trim();
        booking.ReferralClinicalNotes = dto.ClinicalNotes?.Trim();
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _context.Entry(booking).Reference(b => b.ReferredToAgent).LoadAsync();

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("BookingUpdated", ToDto(booking));

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("InternalNoteAdded", new InternalNoteDto(
                handoverNote.Id,
                handoverNote.BookingId,
                handoverNote.AgentId,
                referringAgentName,
                handoverNote.Content,
                handoverNote.CreatedAt));

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            await _notificationService.NotifyAsync(
                admin.Id, "ReferralApprovalRequested",
                $"Referral approval required: Dr. {referringAgentName} requested transfer of consultation #{booking.Id.ToString()[..8]} to Dr. {other.FullName}. Reason: {dto.Reason}.", booking.Id);
        }

        return Ok(ToDto(booking));
    }
}

[Authorize(Roles = "Admin")]
[HttpPatch("{id:guid}/approve-referral")]
public async Task<ActionResult<BookingDto>> ApproveReferral(Guid id)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    if (!booking.IsReferralPendingApproval || !booking.ReferredToAgentId.HasValue)
        return BadRequest("There is no pending referral request for this booking.");

    var targetAgent = await _context.Agents.FindAsync(booking.ReferredToAgentId.Value);
    if (targetAgent is null || !targetAgent.IsActive)
        return BadRequest("Target specialist is not available or inactive.");

    var referringAgentName = booking.Agent?.FullName ?? "Previous Specialist";
    var newAgentId = targetAgent.Id;

    booking.AgentId = newAgentId;
    booking.Status = BookingStatus.Assigned;
    booking.IsReferralPendingApproval = false;
    var handoverReason = booking.ReferralReason;
    booking.ReferredToAgentId = null;
    booking.ReferralReason = null;
    booking.ReferralClinicalNotes = null;
    booking.UpdatedAt = DateTime.UtcNow;

    var approvalNote = new InternalNote
    {
        BookingId = booking.Id,
        AgentId = newAgentId,
        Content = $"[Clinical Referral Approved by Admin]\nCase approved and transferred from {referringAgentName} to Dr. {targetAgent.FullName} ({targetAgent.Department}).",
        CreatedAt = DateTime.UtcNow
    };
    _context.InternalNotes.Add(approvalNote);

    await _context.SaveChangesAsync();
    await _context.Entry(booking).Reference(b => b.Agent).LoadAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("InternalNoteAdded", new InternalNoteDto(
            approvalNote.Id,
            approvalNote.BookingId,
            approvalNote.AgentId,
            "Admin",
            approvalNote.Content,
            approvalNote.CreatedAt));

    var targetUserId = await _notificationService.GetUserIdForAgentAsync(targetAgent.Id);
    if (targetUserId is not null)
    {
        await _notificationService.NotifyAsync(
            targetUserId, "BookingAssigned",
            $"Referral approved: You have been assigned consultation for {booking.Patient.FullName} ({booking.ServiceCategory.Name}). Reason: {handoverReason}.", booking.Id);
    }

    var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
    if (patientUserId is not null)
    {
        await _notificationService.NotifyAsync(
            patientUserId, "BookingReferred",
            $"Your consultation has been transferred to specialist Dr. {targetAgent.FullName} following clinical review.", booking.Id);
    }

    return Ok(ToDto(booking));
}

[Authorize(Roles = "Admin")]
[HttpPatch("{id:guid}/reject-referral")]
public async Task<ActionResult<BookingDto>> RejectReferral(Guid id, [FromBody] RejectReferralDto? dto)
{
    var booking = await LoadBooking(id);
    if (booking is null) return NotFound();

    if (!booking.IsReferralPendingApproval)
        return BadRequest("There is no pending referral request for this booking.");

    var referringAgentId = booking.AgentId;
    var targetAgentName = booking.ReferredToAgent?.FullName ?? "Proposed Specialist";
    var rejectReason = string.IsNullOrWhiteSpace(dto?.Reason) ? "Administrative clinical discretion" : dto.Reason.Trim();

    booking.IsReferralPendingApproval = false;
    booking.ReferredToAgentId = null;
    booking.ReferralReason = null;
    booking.ReferralClinicalNotes = null;
    booking.UpdatedAt = DateTime.UtcNow;

    var rejectionNote = new InternalNote
    {
        BookingId = booking.Id,
        AgentId = referringAgentId ?? Guid.Empty,
        Content = $"[Clinical Referral Rejected by Admin]\nReferral to Dr. {targetAgentName} was declined by Admin. Reason: {rejectReason}.",
        CreatedAt = DateTime.UtcNow
    };
    _context.InternalNotes.Add(rejectionNote);

    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("BookingUpdated", ToDto(booking));

    await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
        .SendAsync("InternalNoteAdded", new InternalNoteDto(
            rejectionNote.Id,
            rejectionNote.BookingId,
            rejectionNote.AgentId,
            "Admin",
            rejectionNote.Content,
            rejectionNote.CreatedAt));

    if (referringAgentId.HasValue)
    {
        var referringUserId = await _notificationService.GetUserIdForAgentAsync(referringAgentId.Value);
        if (referringUserId is not null)
        {
            await _notificationService.NotifyAsync(
                referringUserId, "ReferralRejected",
                $"Your referral request for booking #{booking.Id.ToString()[..8]} was declined by Admin. Reason: {rejectReason}.", booking.Id);
        }
    }

    return Ok(ToDto(booking));
}

private async Task<Booking?> LoadBooking(Guid id) =>
    await _context.Bookings
        .Include(b => b.Patient)
        .Include(b => b.ServiceCategory)
        .Include(b => b.Agent)
        .Include(b => b.ReferredToAgent)
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
    b.CreatedAt,
    b.IsReferralPendingApproval,
    b.ReferredToAgentId,
    b.ReferredToAgent?.FullName,
    b.ReferralReason,
    b.ReferralClinicalNotes);

    [Authorize(Roles = "Agent,Admin")]
    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<BookingDto>> Complete(Guid id)
    {
        var booking = await LoadBooking(id);
        if (booking is null) return NotFound();

        if (booking.IsReferralPendingApproval)
            return BadRequest("Cannot complete consultation while a referral approval is pending.");

        if (User.IsInRole("Agent") && !User.IsInRole("Admin"))
        {
            var agentId = GetCurrentAgentId();
            if (agentId is null || booking.AgentId != agentId)
                return Forbid();
        }

        if (booking.Status == BookingStatus.Completed)
            return Ok(ToDto(booking));

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(BookingHub.GroupName(booking.Id))
            .SendAsync("BookingUpdated", ToDto(booking));

        var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
        if (patientUserId is not null)
        {
            await _notificationService.NotifyAsync(
                patientUserId, "BookingCompleted",
                "Your consultation has been completed. Please take a moment to rate and review your care experience.", booking.Id);
        }

        return Ok(ToDto(booking));
    }

private static readonly BookingStatus[] AdminAssignableStatuses =
{
    BookingStatus.PendingPayment,
    BookingStatus.Paid,
    BookingStatus.Assigned,
    BookingStatus.InProgress,
    BookingStatus.Completed,
    BookingStatus.Cancelled
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

    var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
    if (patientUserId is not null)
    {
        await _notificationService.NotifyAsync(
            patientUserId, "BookingStatusChanged",
            $"Your booking status changed to {booking.Status}.", booking.Id);
    }
    
    return Ok(ToDto(booking));
}
}