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
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(120)] string NameAmharic,
    [property: StringLength(1000)] string? Description,
    [property: Range(typeof(decimal), "0.01", "1000000")] decimal BasePrice
);

public record UpdateServiceCategoryDto(
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(120)] string NameAmharic,
    [property: StringLength(1000)] string? Description,
    [property: Range(typeof(decimal), "0.01", "1000000")] decimal BasePrice,
    bool IsActive
);
