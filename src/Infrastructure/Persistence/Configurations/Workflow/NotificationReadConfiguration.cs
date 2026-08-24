using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class NotificationReadConfiguration : BaseModelConfig, IEntityTypeConfiguration<NotificationRead>
{
    public void Configure(EntityTypeBuilder<NotificationRead> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        // Idempotent mark-as-read: a second mark for the same (notification, user) is a no-op,
        // enforced here rather than relying only on the application-layer existence check.
        builder.HasIndex(r => new { r.NotificationId, r.UserId }).IsUnique();
    }
}
