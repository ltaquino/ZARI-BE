using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ThirteenthMonthPayLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<ThirteenthMonthPayLine>
{
    public void Configure(EntityTypeBuilder<ThirteenthMonthPayLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TotalBasicPay).HasColumnType(DefaultDecimal);
        builder.Property(l => l.ThirteenthMonthPay).HasColumnType(DefaultDecimal);

        builder.HasIndex(l => new { l.ThirteenthMonthPayRunId, l.EmployeeId }).IsUnique();

        builder.HasOne(l => l.Employee).WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
