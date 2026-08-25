using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration : BaseModelConfig, IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT).ValueGeneratedNever();

        builder.Property(c => c.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Name).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
