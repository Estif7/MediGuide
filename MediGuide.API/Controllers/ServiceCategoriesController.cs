using MediGuide.Application.DTOs;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly MediGuideDbContext _context;

    public ServiceCategoriesController(MediGuideDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceCategoryDto>>> GetAll()
    {
        var categories = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.NameAmharic,
                c.Description,
                c.BasePrice,
                c.IsActive))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceCategoryDto>> GetById(Guid id)
    {
        var category = await _context.ServiceCategories
            .Where(c => c.Id == id)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.NameAmharic,
                c.Description,
                c.BasePrice,
                c.IsActive))
            .FirstOrDefaultAsync();

        if (category is null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceCategoryDto>> Create(CreateServiceCategoryDto dto)
    {
        var category = new Domain.Entities.ServiceCategory
        {
            Name = dto.Name,
            NameAmharic = dto.NameAmharic,
            Description = dto.Description,
            BasePrice = dto.BasePrice
        };

        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync();

        var result = new ServiceCategoryDto(
            category.Id,
            category.Name,
            category.NameAmharic,
            category.Description,
            category.BasePrice,
            category.IsActive);

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, result);
    }
}