using MediGuide.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediGuide.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(token => token.ApplicationUserId).HasMaxLength(450).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.RevokedReason).HasMaxLength(200);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.ApplicationUserId, token.ExpiresAt });

        builder.HasOne(token => token.ApplicationUser)
            .WithMany()
            .HasForeignKey(token => token.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
