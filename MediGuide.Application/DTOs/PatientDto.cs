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
    [property: Required, StringLength(150)] string FullName,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(32)] string PhoneNumber,
    [property: StringLength(10)] string? PreferredLanguage
);
