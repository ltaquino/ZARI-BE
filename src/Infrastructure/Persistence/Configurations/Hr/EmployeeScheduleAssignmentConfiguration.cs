using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class EmployeeScheduleAssignmentConfiguration : BaseModelConfig, IEntityTypeConfiguration<EmployeeScheduleAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeScheduleAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.WorkScheduleId);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.WorkSchedule)
            .WithMany()
            .HasForeignKey(a => a.WorkScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
