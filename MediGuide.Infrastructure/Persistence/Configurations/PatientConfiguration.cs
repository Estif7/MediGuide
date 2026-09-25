using MediGuide.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediGuide.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(150);
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(30);

        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.EmergencyContactName).HasMaxLength(150);
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(x => x.Allergies).HasMaxLength(1000);
        builder.Property(x => x.ChronicConditions).HasMaxLength(1000);
        builder.Property(x => x.CurrentMedications).HasMaxLength(1000);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}