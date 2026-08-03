using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid BookingId { get; set; }
    public string SenderId { get; set; } = string.Empty;      // Patient or Agent Id (string for flexibility)
    public string SenderRole { get; set; } = string.Empty;    // "Patient" or "Agent"
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    // Navigation
    public Booking Booking { get; set; } = null!;
}