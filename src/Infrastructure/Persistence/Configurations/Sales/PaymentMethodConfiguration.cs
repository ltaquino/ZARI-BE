using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PaymentMethodConfiguration : BaseModelConfig, IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(m => m.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(m => m.ReferenceNoLabel).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(m => m.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(m => m.Code).IsUnique();

        builder.HasOne(m => m.GlAccount)
            .WithMany()
            .HasForeignKey(m => m.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
