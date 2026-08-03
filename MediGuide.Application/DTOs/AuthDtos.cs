namespace MediGuide.Application.DTOs;

public record RegisterPatientDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? PreferredLanguage
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    string Email,
    string FullName,
    IEnumerable<string> Roles,
    Guid? PatientId,
    Guid? AgentId
);