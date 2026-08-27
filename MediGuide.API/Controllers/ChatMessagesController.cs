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

    public ChatMessagesController(
        MediGuideDbContext context,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    // List messages for a booking (oldest first)
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetByBooking(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, booking, "BookingAccess");
        if (!authorizationResult.Succeeded)
            return Forbid();

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.BookingId == bookingId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.BookingId,
                m.SenderId,
                m.SenderRole,
                m.Content,
                m.IsRead,
                m.CreatedAt))
            .ToListAsync();

        return Ok(messages);
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
