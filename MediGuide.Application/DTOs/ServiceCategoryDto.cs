using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string NameAmharic,
    string? Description,
    decimal BasePrice,
    bool IsActive
);

public record CreateServiceCategoryDto(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(120)] string NameAmharic,
    [StringLength(1000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal BasePrice
);

public record UpdateServiceCategoryDto(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(120)] string NameAmharic,
    [StringLength(1000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal BasePrice,
    bool IsActive
);
