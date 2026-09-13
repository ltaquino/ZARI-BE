using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PayrollRunConfiguration : BaseModelConfig, IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PayrollRunNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(r => r.PayrollRunNo).IsUnique();
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines).WithOne(l => l.PayrollRun).HasForeignKey(l => l.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
    }
}
