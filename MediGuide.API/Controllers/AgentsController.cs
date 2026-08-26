using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly MediGuideDbContext _context;

    public AgentsController(MediGuideDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Agent")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAll()
    {
        var query = _context.Agents.Where(a => a.IsActive);

        if (!User.IsInRole("Admin"))
        {
            if (!Guid.TryParse(User.FindFirst("agentId")?.Value, out var callerAgentId))
                return Forbid();

            query = query.Where(a => a.Id == callerAgentId);
        }

        var agents = await query
            .OrderBy(a => a.FullName)
            .Select(a => new AgentDto(
                a.Id,
                a.FullName,
                a.Email,
                a.PhoneNumber,
                a.IsAvailable,
                a.IsActive))
            .ToListAsync();

        return Ok(agents);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> GetById(Guid id)
    {
        if (!User.IsInRole("Admin")
            && (!Guid.TryParse(User.FindFirst("agentId")?.Value, out var agentId) || agentId != id))
        {
            return Forbid();
        }

        var agent = await _context.Agents
            .Where(a => a.Id == id)
            .Select(a => new AgentDto(
                a.Id,
                a.FullName,
                a.Email,
                a.PhoneNumber,
                a.IsAvailable,
                a.IsActive))
            .FirstOrDefaultAsync();

        if (agent is null)
            return NotFound();

        return Ok(agent);
    }
}