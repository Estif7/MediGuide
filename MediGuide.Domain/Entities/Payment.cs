using MediGuide.Domain.Common;
using MediGuide.Domain.Enums;

namespace MediGuide.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string TxRef { get; set; } = string.Empty;          // CHAPA transaction reference
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ETB";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ChapaCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
}