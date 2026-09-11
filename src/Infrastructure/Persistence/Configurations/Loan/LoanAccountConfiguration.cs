using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanAccountConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanAccount>
{
    public void Configure(EntityTypeBuilder<LoanAccount> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.LoanAcctNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.PrincipalAmount).HasColumnType(DefaultDecimal);
        builder.Property(a => a.AnnualInterestRatePct).HasColumnType(DefaultDecimal);
        builder.Property(a => a.RepaymentFrequency).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.PenaltyRatePct).HasColumnType(DefaultDecimal);
        builder.Property(a => a.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(a => a.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(a => a.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(a => a.LoanAcctNo).IsUnique();
        builder.HasIndex(a => new { a.BranchId, a.GrantDate });
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.LoanApplicationId);

        builder.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.LoanProduct).WithMany().HasForeignKey(a => a.LoanProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.LoanApplication).WithMany().HasForeignKey(a => a.LoanApplicationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.LoanReceivableAccount).WithMany().HasForeignKey(a => a.LoanReceivableAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.InterestIncomeAccount).WithMany().HasForeignKey(a => a.InterestIncomeAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PenaltyIncomeAccount).WithMany().HasForeignKey(a => a.PenaltyIncomeAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ScheduleLines).WithOne(s => s.LoanAccount).HasForeignKey(s => s.LoanAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
