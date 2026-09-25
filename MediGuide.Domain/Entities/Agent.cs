using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Agent : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Title { get; set; } // e.g. "MD, Consultant Pediatrician"
    public string? Department { get; set; } // e.g. "Pediatrics", "Mental Health", "Cardiology"
    public string? Specialty { get; set; } // e.g. "Neonatology & Child Health"
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Booking> AssignedBookings { get; set; } = new List<Booking>();
}