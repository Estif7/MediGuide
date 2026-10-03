using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record RegisterPatientDto(
    [Required, StringLength(150)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(32)] string PhoneNumber,
    [Required, StringLength(256, MinimumLength = 8)] string Password,
    [StringLength(10)] string? PreferredLanguage,
    DateTime? DateOfBirth = null,
    [StringLength(20)] string? Gender = null,
    [StringLength(150)] string? EmergencyContactName = null,
    [StringLength(30)] string? EmergencyContactPhone = null,
    [StringLength(1000)] string? Allergies = null,
    [StringLength(1000)] string? ChronicConditions = null,
    [StringLength(1000)] string? CurrentMedications = null
);

public record LoginDto(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256)] string Password
);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string Email,
    string FullName,
    IEnumerable<string> Roles,
    Guid? PatientId,
    Guid? AgentId
);

public record RefreshTokenRequestDto(
    [Required, StringLength(512)] string RefreshToken
);

public record RegisterAgentDto(
    [Required, StringLength(150)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(32)] string PhoneNumber,
    [Required, StringLength(256, MinimumLength = 8)] string Password
);