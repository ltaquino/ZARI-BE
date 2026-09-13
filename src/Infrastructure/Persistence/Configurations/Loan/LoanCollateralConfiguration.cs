using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanCollateralConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanCollateral>
{
    public void Configure(EntityTypeBuilder<LoanCollateral> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.CollateralType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.EstimatedValue).HasColumnType(DefaultDecimal);
        builder.Property(c => c.DocumentRef).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
    }
}
