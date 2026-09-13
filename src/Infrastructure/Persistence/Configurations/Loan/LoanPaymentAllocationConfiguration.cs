using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanPaymentAllocationConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<LoanPaymentAllocation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PrincipalApplied).HasColumnType(DefaultDecimal);
        builder.Property(a => a.InterestApplied).HasColumnType(DefaultDecimal);
        builder.Property(a => a.PenaltyApplied).HasColumnType(DefaultDecimal);

        builder.HasIndex(a => a.LoanPaymentId);
        builder.HasIndex(a => a.LoanAmortizationScheduleLineId);

        builder.HasOne(a => a.LoanAmortizationScheduleLine).WithMany().HasForeignKey(a => a.LoanAmortizationScheduleLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
