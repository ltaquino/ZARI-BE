using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class EmployeeDeductionConfiguration : BaseModelConfig, IEntityTypeConfiguration<EmployeeDeduction>
{
    public void Configure(EntityTypeBuilder<EmployeeDeduction> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(d => d.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(d => d.Amount).HasColumnType(DefaultDecimal);
        builder.Property(d => d.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(d => d.EmployeeId);

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
