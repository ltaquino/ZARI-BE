using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class TaxCodeConfiguration : BaseModelConfig, IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> builder)
    {
        builder.HasKey(t => t.Code);
        builder.Property(t => t.Code).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.Rate).HasColumnType(DefaultDecimal);
        builder.Property(t => t.TaxType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(t => t.GlAccount)
            .WithMany()
            .HasForeignKey(t => t.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
