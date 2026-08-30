using System.Security.Claims;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuide.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using MediGuide.API.Services;
namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatMessagesController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHubContext<BookingHub> _hubContext;
    private readonly INotificationService _notificationService;

    private const int MaxChatPageSize = 100;

    public ChatMessagesController(
        MediGuideDbContext context,
        IAuthorizationService authorizationService,
        IHubContext<BookingHub> hubContext,
        INotificationService notificationService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _hubContext = hubContext;
        _notificationService = notificationService;
    }

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<CursorPagedResult<ChatMessageDto>>> GetByBooking(
        Guid bookingId, [FromQuery] ChatMessageQueryParams query)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        var pageSize = query.PageSize < 1 ? 30 : Math.Min(query.PageSize, MaxChatPageSize);

        var messagesQuery = _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.BookingId == bookingId);

        if (query.Before.HasValue)
            messagesQuery = messagesQuery.Where(m => m.CreatedAt < query.Before.Value);

        // Fetch newest-first, one extra to detect "more older messages exist"
        var fetched = await messagesQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(pageSize + 1)
            .Select(m => new ChatMessageDto(
                m.Id, m.BookingId, m.SenderId, m.SenderRole, m.Content, m.IsRead, m.CreatedAt))
            .ToListAsync();

        var hasMore = fetched.Count > pageSize;
        var page = fetched.Take(pageSize).OrderBy(m => m.CreatedAt).ToList(); // ascending for display

        return Ok(new CursorPagedResult<ChatMessageDto>(page, hasMore));
    }

    // Send a message
    [HttpPost("booking/{bookingId:guid}")]
    public async Task<ActionResult<ChatMessageDto>> Send(Guid bookingId, CreateChatMessageDto dto)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? "unknown";

        var role = User.IsInRole("Agent") ? "Agent"
                 : User.IsInRole("Admin") ? "Admin"
                 : "Patient";

        var message = new ChatMessage
        {
            BookingId = bookingId,
            SenderId = userId,
            SenderRole = role,
            Content = dto.Content.Trim(),
            IsRead = false
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var result = new ChatMessageDto(
            message.Id,
            message.BookingId,
            message.SenderId,
            message.SenderRole,
            message.Content,
            message.IsRead,
            message.CreatedAt);

        await _hubContext.Clients.Group(BookingHub.GroupName(bookingId))
            .SendAsync("ReceiveMessage", result);

        var patientUserId = await _notificationService.GetUserIdForPatientAsync(booking.PatientId);
        var agentUserId = booking.AgentId.HasValue
            ? await _notificationService.GetUserIdForAgentAsync(booking.AgentId.Value)
            : null;

        var recipientId = patientUserId != userId ? patientUserId : agentUserId;

        if (recipientId is not null && !BookingHub.IsUserViewingBooking(bookingId, recipientId))
        {
            var preview = message.Content.Length > 80 ? message.Content[..80] + "…" : message.Content;
            await _notificationService.NotifyAsync(
                recipientId, "ChatMessage", $"New message on your booking: {preview}", bookingId);
        }

        return CreatedAtAction(nameof(GetByBooking), new { bookingId }, result);
    }

    private async Task<string?> GetOtherParticipantUserIdAsync(Booking booking, string senderUserId)
    {
        // Find the ApplicationUser tied to the patient, and the one tied to the assigned agent (if any).
        var patientUser = await _context.Users.FirstOrDefaultAsync(u => u.PatientId == booking.PatientId);
        var agentUser = booking.AgentId.HasValue
            ? await _context.Users.FirstOrDefaultAsync(u => u.AgentId == booking.AgentId)
            : null;

        if (patientUser?.Id != senderUserId) return patientUser?.Id;
        return agentUser?.Id;
    }
}
