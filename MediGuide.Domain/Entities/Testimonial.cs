using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Testimonial : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? BookingId { get; set; }
    public int Rating { get; set; } = 5; // 1 to 5 stars
    public string Comment { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = false;

    // Navigation
    public Patient Patient { get; set; } = null!;
    public Booking? Booking { get; set; }
}
