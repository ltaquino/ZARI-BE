using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class WithholdingTaxBracketConfiguration : BaseModelConfig, IEntityTypeConfiguration<WithholdingTaxBracket>
{
    public void Configure(EntityTypeBuilder<WithholdingTaxBracket> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.SalaryFrom).HasColumnType(DefaultDecimal);
        builder.Property(w => w.SalaryTo).HasColumnType(DefaultDecimal);
        builder.Property(w => w.BaseTax).HasColumnType(DefaultDecimal);
        builder.Property(w => w.RateOverExcess).HasColumnType(DefaultDecimal);
        builder.Property(w => w.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(w => new { w.EffectiveDate, w.SalaryFrom });
    }
}
