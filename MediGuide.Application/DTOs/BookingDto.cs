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
    [Required] Guid PatientId,
    [Required] Guid ServiceCategoryId,
    ResponseTime ResponseTime,
    [StringLength(2000)] string? Notes
);

public record UpdateBookingStatusDto(
    [Required] BookingStatus Status
);

public record BookingQueryParams(
    int Page = 1,
    int PageSize = 20,
    BookingStatus? Status = null,
    string? PatientName = null,
    string? AgentName = null,
    string SortBy = "CreatedAt",
    string SortDir = "desc"
);