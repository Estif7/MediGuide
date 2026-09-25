namespace MediGuide.Application.DTOs;

public record UserProfileDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    IList<string> Roles,
    Guid? PatientId,
    Guid? AgentId,
    DateTime CreatedAt,
    string? Title = null,
    string? Department = null,
    string? Specialty = null,
    bool? IsAvailable = null);

public record UpdateUserProfileDto(
    string FullName,
    string? PhoneNumber,
    string? CurrentPassword = null,
    string? NewPassword = null,
    string? Title = null,
    string? Department = null,
    string? Specialty = null,
    bool? IsAvailable = null);

public record AdminUserDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    IList<string> Roles,
    bool IsActive,
    DateTime CreatedAt,
    Guid? PatientId,
    Guid? AgentId,
    string? Title = null,
    string? Department = null,
    string? Specialty = null);

public record AdminCreateUserDto(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber,
    string Role,
    string? Title = null,
    string? Department = null,
    string? Specialty = null);

public record AdminUpdateUserDto(
    string FullName,
    string? PhoneNumber,
    string? Role,
    bool? IsActive,
    string? Title = null,
    string? Department = null,
    string? Specialty = null);

public record UserQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null);
