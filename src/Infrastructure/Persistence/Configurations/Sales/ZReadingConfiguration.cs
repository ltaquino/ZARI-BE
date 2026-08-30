using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ZReadingConfiguration : BaseModelConfig, IEntityTypeConfiguration<ZReading>
{
    public void Configure(EntityTypeBuilder<ZReading> builder)
    {
        builder.HasKey(z => z.Id);

        builder.Property(z => z.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(z => z.FirstOrNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(z => z.LastOrNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(z => z.GrossSales).HasColumnType(DefaultDecimal);
        builder.Property(z => z.TotalDiscounts).HasColumnType(DefaultDecimal);
        builder.Property(z => z.VatableSales).HasColumnType(DefaultDecimal);
        builder.Property(z => z.VatAmount).HasColumnType(DefaultDecimal);
        builder.Property(z => z.VatExemptSales).HasColumnType(DefaultDecimal);
        builder.Property(z => z.ZeroRatedSales).HasColumnType(DefaultDecimal);
        builder.Property(z => z.NetSales).HasColumnType(DefaultDecimal);

        // One ZReading per (branch, counter value) — the permanent, unbroken sequence.
        builder.HasIndex(z => new { z.BranchId, z.ZCounterValue }).IsUnique();

        builder.HasOne(z => z.Branch)
            .WithMany()
            .HasForeignKey(z => z.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
