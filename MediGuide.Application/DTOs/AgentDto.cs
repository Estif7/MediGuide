using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record AgentDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? Title,
    string? Department,
    string? Specialty,
    bool IsAvailable,
    bool IsActive
);

public record AgentQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Name = null,
    string? Department = null,
    string? Specialty = null,
    Guid? ExcludeAgentId = null
);