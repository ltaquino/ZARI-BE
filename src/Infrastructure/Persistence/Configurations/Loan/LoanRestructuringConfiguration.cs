using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanRestructuringConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanRestructuring>
{
    public void Configure(EntityTypeBuilder<LoanRestructuring> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RestructuringNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.OldPrincipalBalance).HasColumnType(DefaultDecimal);
        builder.Property(r => r.NewAnnualInterestRatePct).HasColumnType(DefaultDecimal);
        builder.Property(r => r.NewRepaymentFrequency).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.NewPenaltyRatePct).HasColumnType(DefaultDecimal);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.RestructuringNo).IsUnique();
        builder.HasIndex(r => new { r.BranchId, r.RestructureDate });
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.OldLoanAccountId);

        builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.OldLoanAccount).WithMany().HasForeignKey(r => r.OldLoanAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.NewLoanAccount).WithMany().HasForeignKey(r => r.NewLoanAccountId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
