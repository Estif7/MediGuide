using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Agent : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Booking> AssignedBookings { get; set; } = new List<Booking>();
}