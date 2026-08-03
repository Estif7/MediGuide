using MediGuide.Domain.Common;
using MediGuide.Domain.Enums;

namespace MediGuide.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ServiceCategoryId { get; set; }
    public Guid? AgentId { get; set; }                 // null until assigned

    public ResponseTime ResponseTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public decimal Amount { get; set; }                // snapshot of price at booking time
    public string? Notes { get; set; }                 // patient notes

    // Navigation
    public Patient Patient { get; set; } = null!;
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public Agent? Agent { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<InternalNote> InternalNotes { get; set; } = new List<InternalNote>();
}