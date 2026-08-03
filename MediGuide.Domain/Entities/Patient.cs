using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Patient : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; } = "en"; // "en" or "am"
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}