using MediGuide.Domain.Enums;

namespace MediGuide.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid ServiceCategoryId,
    string CategoryName,
    Guid? AgentId,
    string? AgentName,
    ResponseTime ResponseTime,
    BookingStatus Status,
    decimal Amount,
    string? Notes,
    DateTime CreatedAt
);

public record CreateBookingDto(
    Guid PatientId,
    Guid ServiceCategoryId,
    ResponseTime ResponseTime,
    string? Notes
);