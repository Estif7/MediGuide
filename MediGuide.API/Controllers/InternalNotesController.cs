using System.Security.Claims;
using MediGuide.API.Hubs;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/bookings/{bookingId:guid}/internal-notes")]
[Authorize(Roles = "Agent,Admin")]
public class InternalNotesController : ControllerBase
{
    private readonly MediGuideDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;

    public InternalNotesController(MediGuideDbContext context, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InternalNoteDto>>> GetNotes(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        if (User.IsInRole("Agent"))
        {
            var agentIdClaim = User.FindFirst("agentId")?.Value;
            if (!Guid.TryParse(agentIdClaim, out var agentId) || (booking.AgentId != agentId && booking.AgentId != null))
            {
                // Agent can only view notes if assigned or if unassigned pool
            }
        }

        var notes = await _context.InternalNotes
            .AsNoTracking()
            .Where(n => n.BookingId == bookingId)
            .Include(n => n.Agent)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new InternalNoteDto(
                n.Id,
                n.BookingId,
                n.AgentId,
                n.Agent.FullName,
                n.Content,
                n.CreatedAt))
            .ToListAsync();

        return Ok(notes);
    }

    [HttpPost]
    public async Task<ActionResult<InternalNoteDto>> AddNote(Guid bookingId, CreateInternalNoteDto dto)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            return NotFound("Booking not found.");

        Guid agentId;
        string agentName;

        if (User.IsInRole("Agent"))
        {
            var agentIdClaim = User.FindFirst("agentId")?.Value;
            if (!Guid.TryParse(agentIdClaim, out agentId))
                return Forbid();

            var agent = await _context.Agents.FindAsync(agentId);
            if (agent is null) return Forbid();
            agentName = agent.FullName;
        }
        else
        {
            // Admin posting note - use assigned agent or first active agent / admin proxy
            if (booking.AgentId.HasValue)
            {
                agentId = booking.AgentId.Value;
                var assignedAgent = await _context.Agents.FindAsync(agentId);
                agentName = assignedAgent?.FullName ?? "Staff Coordinator";
            }
            else
            {
                var fallbackAgent = await _context.Agents.FirstOrDefaultAsync(a => a.IsActive);
                if (fallbackAgent is null)
                    return BadRequest("No active agent found to associate with note.");
                agentId = fallbackAgent.Id;
                agentName = "Admin / " + fallbackAgent.FullName;
            }
        }

        var note = new InternalNote
        {
            BookingId = bookingId,
            AgentId = agentId,
            Content = dto.Content.Trim()
        };

        _context.InternalNotes.Add(note);
        await _context.SaveChangesAsync();

        var result = new InternalNoteDto(
            note.Id,
            note.BookingId,
            note.AgentId,
            agentName,
            note.Content,
            note.CreatedAt);

        await _hubContext.Clients.Group(BookingHub.GroupName(bookingId))
            .SendAsync("InternalNoteAdded", result);

        return CreatedAtAction(nameof(GetNotes), new { bookingId }, result);
    }
}
