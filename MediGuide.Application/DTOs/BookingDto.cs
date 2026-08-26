using MediGuide.Domain.Enums;
using System.ComponentModel.DataAnnotations;

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
    [property: Required] Guid PatientId,
    [property: Required] Guid ServiceCategoryId,
    ResponseTime ResponseTime,
    [property: StringLength(2000)] string? Notes
);
