using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GovernmentContributionRateConfiguration : BaseModelConfig, IEntityTypeConfiguration<GovernmentContributionRate>
{
    public void Configure(EntityTypeBuilder<GovernmentContributionRate> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.ContributionType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(g => g.SalaryFrom).HasColumnType(DefaultDecimal);
        builder.Property(g => g.SalaryTo).HasColumnType(DefaultDecimal);
        builder.Property(g => g.EmployeeSharePct).HasColumnType(DefaultDecimal);
        builder.Property(g => g.EmployerSharePct).HasColumnType(DefaultDecimal);
        builder.Property(g => g.MaxContribution).HasColumnType(DefaultDecimal);
        builder.Property(g => g.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(g => new { g.ContributionType, g.EffectiveDate, g.SalaryFrom });
    }
}
