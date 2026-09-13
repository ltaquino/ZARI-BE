using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanDisbursementConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanDisbursement>
{
    public void Configure(EntityTypeBuilder<LoanDisbursement> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DisbursementNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(d => d.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(d => d.Amount).HasColumnType(DefaultDecimal);
        builder.Property(d => d.ReferenceNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(d => d.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(d => d.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(d => d.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(d => d.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(d => d.DisbursementNo).IsUnique();
        builder.HasIndex(d => new { d.BranchId, d.DisbursementDate });
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.LoanAccountId);

        builder.HasOne(d => d.Branch).WithMany().HasForeignKey(d => d.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.LoanAccount).WithMany().HasForeignKey(d => d.LoanAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.PaymentMethod).WithMany().HasForeignKey(d => d.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.CostCenter).WithMany().HasForeignKey(d => d.CostCenterId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
