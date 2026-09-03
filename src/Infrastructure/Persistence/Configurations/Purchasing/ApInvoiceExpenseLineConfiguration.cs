using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ApInvoiceExpenseLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<ApInvoiceExpenseLine>
{
    public void Configure(EntityTypeBuilder<ApInvoiceExpenseLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(l => l.Amount).HasColumnType(DefaultDecimal);
        builder.Property(l => l.VatType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(l => l.GlAccount)
            .WithMany()
            .HasForeignKey(l => l.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
