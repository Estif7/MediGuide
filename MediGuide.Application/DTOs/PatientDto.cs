using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record PatientDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? PreferredLanguage,
    bool IsActive,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    string? Allergies = null,
    string? ChronicConditions = null,
    string? CurrentMedications = null
);

public record CreatePatientDto(
    [Required, StringLength(150)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(32)] string PhoneNumber,
    [StringLength(10)] string? PreferredLanguage,
    DateTime? DateOfBirth = null,
    [StringLength(20)] string? Gender = null,
    [StringLength(150)] string? EmergencyContactName = null,
    [StringLength(30)] string? EmergencyContactPhone = null,
    [StringLength(1000)] string? Allergies = null,
    [StringLength(1000)] string? ChronicConditions = null,
    [StringLength(1000)] string? CurrentMedications = null
);

public record UpdatePatientProfileDto(
    [Required, StringLength(150)] string FullName,
    [Required, StringLength(32)] string PhoneNumber,
    [StringLength(10)] string? PreferredLanguage,
    DateTime? DateOfBirth,
    [StringLength(20)] string? Gender,
    [StringLength(150)] string? EmergencyContactName,
    [StringLength(30)] string? EmergencyContactPhone,
    [StringLength(1000)] string? Allergies,
    [StringLength(1000)] string? ChronicConditions,
    [StringLength(1000)] string? CurrentMedications
);

public record PatientQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Name = null
);