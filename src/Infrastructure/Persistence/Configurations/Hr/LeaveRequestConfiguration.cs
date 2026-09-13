using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration : BaseModelConfig, IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.RequestNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.Days).HasColumnType(DefaultDecimal);
        builder.Property(l => l.Reason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(l => l.AttachmentUrl).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(l => l.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(l => l.RequestNo).IsUnique();
        builder.HasIndex(l => l.Status);

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
