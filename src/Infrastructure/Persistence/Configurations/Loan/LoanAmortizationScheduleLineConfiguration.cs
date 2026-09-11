using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanAmortizationScheduleLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanAmortizationScheduleLine>
{
    public void Configure(EntityTypeBuilder<LoanAmortizationScheduleLine> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PrincipalDue).HasColumnType(DefaultDecimal);
        builder.Property(s => s.InterestDue).HasColumnType(DefaultDecimal);
        builder.Property(s => s.TotalDue).HasColumnType(DefaultDecimal);
        builder.Property(s => s.OutstandingPrincipalAfter).HasColumnType(DefaultDecimal);
        builder.Property(s => s.PrincipalPaid).HasColumnType(DefaultDecimal);
        builder.Property(s => s.InterestPaid).HasColumnType(DefaultDecimal);
        builder.Property(s => s.PenaltyPaid).HasColumnType(DefaultDecimal);
        builder.Property(s => s.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(s => new { s.LoanAccountId, s.InstallmentNo }).IsUnique();
        builder.HasIndex(s => s.DueDate);
    }
}
