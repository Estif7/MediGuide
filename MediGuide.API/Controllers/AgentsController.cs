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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAll()
    {
        var agents = await _context.Agents
            .Where(a => a.IsActive)
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> GetById(Guid id)
    {
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

    // Only Admin can create agents
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create(CreateAgentDto dto)
    {
        var agent = new Agent
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            IsAvailable = true,
            IsActive = true
        };

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var result = new AgentDto(
            agent.Id,
            agent.FullName,
            agent.Email,
            agent.PhoneNumber,
            agent.IsAvailable,
            agent.IsActive);

        return CreatedAtAction(nameof(GetById), new { id = agent.Id }, result);
    }
}