using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class OvertimeRateRuleConfiguration : BaseModelConfig, IEntityTypeConfiguration<OvertimeRateRule>
{
    public void Configure(EntityTypeBuilder<OvertimeRateRule> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OtType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.Multiplier).HasColumnType(DefaultDecimal);
        builder.Property(o => o.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(o => new { o.OtType, o.EffectiveDate });
    }
}
