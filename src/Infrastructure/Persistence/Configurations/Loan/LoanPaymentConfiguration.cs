using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanPaymentConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.Amount).HasColumnType(DefaultDecimal);
        builder.Property(p => p.ReferenceNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(p => p.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(p => p.PaymentNo).IsUnique();
        builder.HasIndex(p => new { p.BranchId, p.PaymentDate });
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.LoanAccountId);

        builder.HasOne(p => p.Branch).WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.LoanAccount).WithMany().HasForeignKey(p => p.LoanAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PaymentMethod).WithMany().HasForeignKey(p => p.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CostCenter).WithMany().HasForeignKey(p => p.CostCenterId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.HasMany(p => p.Allocations).WithOne(a => a.LoanPayment).HasForeignKey(a => a.LoanPaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}
