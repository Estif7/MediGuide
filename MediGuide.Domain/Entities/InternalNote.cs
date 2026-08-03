using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class InternalNote : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid AgentId { get; set; }
    public string Content { get; set; } = string.Empty;

    // Navigation
    public Booking Booking { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
}