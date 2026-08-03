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
    string FullName,
    string Email,
    string PhoneNumber,
    string? PreferredLanguage
);