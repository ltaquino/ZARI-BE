using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : BaseModelConfig, IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(s => s.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(s => s.TaxId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(s => s.CurrencyId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(s => s.Address).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(s => s.ContactPerson).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(s => s.ContactNumber).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(s => s.Email).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(s => s.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasOne(s => s.Currency)
            .WithMany()
            .HasForeignKey(s => s.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ApAccount)
            .WithMany()
            .HasForeignKey(s => s.ApAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
