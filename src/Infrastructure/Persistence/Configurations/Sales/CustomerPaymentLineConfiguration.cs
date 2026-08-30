using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CustomerPaymentLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<CustomerPaymentLine>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.AmountApplied).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.SalesInvoice)
            .WithMany()
            .HasForeignKey(l => l.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
