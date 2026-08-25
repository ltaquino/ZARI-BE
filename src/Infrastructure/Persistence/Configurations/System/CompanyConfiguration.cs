using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : BaseModelConfig, IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.TaxId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.BaseCurrencyId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(c => c.BaseCurrency)
            .WithMany()
            .HasForeignKey(c => c.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
