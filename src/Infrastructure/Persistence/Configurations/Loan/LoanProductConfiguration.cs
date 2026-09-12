using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanProductConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanProduct>
{
    public void Configure(EntityTypeBuilder<LoanProduct> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.InterestMethod).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.AnnualInterestRatePct).HasColumnType(DefaultDecimal);
        builder.Property(p => p.MinPrincipal).HasColumnType(DefaultDecimal);
        builder.Property(p => p.MaxPrincipal).HasColumnType(DefaultDecimal);
        builder.Property(p => p.RepaymentFrequency).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.PenaltyRatePct).HasColumnType(DefaultDecimal);
        builder.Property(p => p.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.CicContractTypeCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(p => p.LoanReceivableAccount).WithMany().HasForeignKey(p => p.LoanReceivableAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.InterestIncomeAccount).WithMany().HasForeignKey(p => p.InterestIncomeAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PenaltyIncomeAccount).WithMany().HasForeignKey(p => p.PenaltyIncomeAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
