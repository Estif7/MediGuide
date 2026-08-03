using Microsoft.AspNetCore.Identity;

namespace MediGuide.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional links to domain entities
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }
}