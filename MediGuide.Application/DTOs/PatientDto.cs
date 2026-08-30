using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record PatientDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? PreferredLanguage,
    bool IsActive
);

public record CreatePatientDto(
    [Required, StringLength(150)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(32)] string PhoneNumber,
    [StringLength(10)] string? PreferredLanguage
);

public record PatientQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Name = null
);