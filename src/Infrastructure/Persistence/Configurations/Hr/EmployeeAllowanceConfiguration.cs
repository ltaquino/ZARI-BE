using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class EmployeeAllowanceConfiguration : BaseModelConfig, IEntityTypeConfiguration<EmployeeAllowance>
{
    public void Configure(EntityTypeBuilder<EmployeeAllowance> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(a => a.Amount).HasColumnType(DefaultDecimal);
        builder.Property(a => a.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(a => a.EmployeeId);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
