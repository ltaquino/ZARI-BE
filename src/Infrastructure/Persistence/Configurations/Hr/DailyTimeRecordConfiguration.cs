using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class DailyTimeRecordConfiguration : BaseModelConfig, IEntityTypeConfiguration<DailyTimeRecord>
{
    public void Configure(EntityTypeBuilder<DailyTimeRecord> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Source)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(d => d.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        // One DTR row per employee per day, regardless of Source — a kiosk time-out punch updates
        // the same day's row a manual time-in created, rather than creating a second row.
        builder.HasIndex(d => new { d.EmployeeId, d.Date }).IsUnique();

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
