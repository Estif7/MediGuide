using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record AgentDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    bool IsAvailable,
    bool IsActive
);

public record CreateAgentDto(
    [property: Required, StringLength(150)] string FullName,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(32)] string PhoneNumber
);
