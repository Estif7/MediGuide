using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;          // e.g. "Mental Health"
    public string NameAmharic { get; set; } = string.Empty;    // Amharic version
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }                     // always decimal for money
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}