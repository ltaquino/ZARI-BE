using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PurchaseRequestConfiguration : BaseModelConfig, IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.RequestNo).IsUnique();
        builder.HasIndex(r => new { r.BranchId, r.RequestDate });
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.PurchaseRequest)
            .HasForeignKey(l => l.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
