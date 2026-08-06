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

    public ChatMessagesController(MediGuideDbContext context)
    {
        _context = context;
    }

    // List messages for a booking (oldest first)
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetByBooking(Guid bookingId)
    {
        var messages = await _context.ChatMessages
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
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Message cannot be empty.");

        var bookingExists = await _context.Bookings.AnyAsync(b => b.Id == bookingId);
        if (!bookingExists)
            return NotFound("Booking not found.");

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