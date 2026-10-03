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
    DateTime CreatedAt,
    bool IsReferralPendingApproval = false,
    Guid? ReferredToAgentId = null,
    string? ReferredToAgentName = null,
    string? ReferralReason = null,
    string? ReferralClinicalNotes = null
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

public record SimulatePaymentDto(
    string PaymentMethod = "Telebirr",
    string? TransactionReference = null
);

public record ReferBookingDto(
    [Required] Guid TargetAgentId,
    [Required][StringLength(200)] string Reason,
    [StringLength(2000)] string? ClinicalNotes = null
);

public record RejectReferralDto(
    string? Reason = null
);