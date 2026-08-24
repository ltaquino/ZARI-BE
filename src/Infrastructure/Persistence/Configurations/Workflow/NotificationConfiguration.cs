using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : BaseModelConfig, IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.EntityType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(n => n.EntityId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(n => n.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(n => n.Type).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(n => n.Category).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(n => n.Message).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(n => n.ActorUserId).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.HasIndex(n => new { n.EntityType, n.EntityId });

        builder.HasMany(n => n.Reads)
            .WithOne(r => r.Notification)
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Branch)
            .WithMany()
            .HasForeignKey(n => n.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
