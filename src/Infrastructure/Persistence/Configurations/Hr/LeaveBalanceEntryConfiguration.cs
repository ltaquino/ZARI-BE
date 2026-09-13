using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LeaveBalanceEntryConfiguration : BaseModelConfig, IEntityTypeConfiguration<LeaveBalanceEntry>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceEntry> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Type).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.Days).HasColumnType(DefaultDecimal);
        builder.Property(l => l.RunningBalance).HasColumnType(DefaultDecimal);
        builder.Property(l => l.ReferenceTable).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        // GUID.ToString() is 36 chars — matches the same VARCHAR_FOR_150 convention used for
        // ApprovalRequest.EntityId/Notification.EntityId, not the 25-char VARCHARDEFAULT (which a
        // live smoke test caught failing with "Data too long for column 'ReferenceId'").
        builder.Property(l => l.ReferenceId).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.HasIndex(l => new { l.EmployeeId, l.LeaveTypeId, l.Date });

        builder.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LeaveType)
            .WithMany()
            .HasForeignKey(l => l.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
