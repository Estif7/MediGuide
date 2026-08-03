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
    string Name,
    string NameAmharic,
    string? Description,
    decimal BasePrice
);

public record UpdateServiceCategoryDto(
    string Name,
    string NameAmharic,
    string? Description,
    decimal BasePrice,
    bool IsActive
);