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
    string FullName,
    string Email,
    string PhoneNumber
);