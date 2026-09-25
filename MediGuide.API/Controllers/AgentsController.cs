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

    private const int MaxPageSize = 100;

    [Authorize(Roles = "Admin,Agent")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<AgentDto>>> GetAll([FromQuery] AgentQueryParams query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var agentsQuery = _context.Agents.AsNoTracking().Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            agentsQuery = agentsQuery.Where(a => EF.Functions.ILike(a.FullName, $"%{query.Name}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            agentsQuery = agentsQuery.Where(a => a.Department != null && EF.Functions.ILike(a.Department, $"%{query.Department}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Specialty))
        {
            agentsQuery = agentsQuery.Where(a => a.Specialty != null && EF.Functions.ILike(a.Specialty, $"%{query.Specialty}%"));
        }

        if (query.ExcludeAgentId.HasValue)
        {
            agentsQuery = agentsQuery.Where(a => a.Id != query.ExcludeAgentId.Value);
        }

        var totalCount = await agentsQuery.CountAsync();

        var agents = await agentsQuery
            .OrderBy(a => a.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AgentDto(
                a.Id,
                a.FullName,
                a.Email,
                a.PhoneNumber,
                a.Title,
                a.Department,
                a.Specialty,
                a.IsAvailable,
                a.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<AgentDto>(agents, totalCount, page, pageSize));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> GetById(Guid id)
    {
        var agent = await _context.Agents
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AgentDto(
                a.Id,
                a.FullName,
                a.Email,
                a.PhoneNumber,
                a.Title,
                a.Department,
                a.Specialty,
                a.IsAvailable,
                a.IsActive))
            .FirstOrDefaultAsync();

        if (agent is null)
            return NotFound();

        return Ok(agent);
    }
}