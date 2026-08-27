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
[Authorize]
public class ChatMessagesController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private const int MaxChatPageSize = 100;
    public ChatMessagesController(
        MediGuideDbContext context,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
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

        return CreatedAtAction(nameof(GetByBooking), new { bookingId }, result);
    }
}
