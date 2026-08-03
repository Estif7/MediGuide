using MediGuide.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.Infrastructure.Persistence;

public class MediGuideDbContext : IdentityDbContext<ApplicationUser>
{
    public MediGuideDbContext(DbContextOptions<MediGuideDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediGuideDbContext).Assembly);
    }
}