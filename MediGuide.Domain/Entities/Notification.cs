using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Notification : BaseEntity
{
    public string RecipientUserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public bool IsRead { get; set; } = false;

    public ApplicationUser RecipientUser { get; set; } = null!;
    public Booking? Booking { get; set; }
}