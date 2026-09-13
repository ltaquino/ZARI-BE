using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SssContributionBracketConfiguration : BaseModelConfig, IEntityTypeConfiguration<SssContributionBracket>
{
    public void Configure(EntityTypeBuilder<SssContributionBracket> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SalaryFrom).HasColumnType(DefaultDecimal);
        builder.Property(s => s.SalaryTo).HasColumnType(DefaultDecimal);
        builder.Property(s => s.EmployeeShare).HasColumnType(DefaultDecimal);
        builder.Property(s => s.EmployerShare).HasColumnType(DefaultDecimal);
        builder.Property(s => s.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(s => new { s.EffectiveDate, s.SalaryFrom });
    }
}
