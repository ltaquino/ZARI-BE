using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StatutoryDiscountTypeConfiguration : BaseModelConfig, IEntityTypeConfiguration<StatutoryDiscountType>
{
    public void Configure(EntityTypeBuilder<StatutoryDiscountType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.DiscountPct).HasColumnType(DefaultDecimal);
        builder.Property(t => t.RequiredIdLabel).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(t => t.Code).IsUnique();
    }
}
