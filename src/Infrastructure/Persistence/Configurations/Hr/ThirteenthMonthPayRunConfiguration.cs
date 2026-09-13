using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ThirteenthMonthPayRunConfiguration : BaseModelConfig, IEntityTypeConfiguration<ThirteenthMonthPayRun>
{
    public void Configure(EntityTypeBuilder<ThirteenthMonthPayRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RunNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(r => r.RunNo).IsUnique();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.BranchId, r.Year }).IsUnique();

        builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines).WithOne(l => l.ThirteenthMonthPayRun).HasForeignKey(l => l.ThirteenthMonthPayRunId).OnDelete(DeleteBehavior.Cascade);
    }
}
