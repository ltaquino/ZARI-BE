using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class OutgoingPaymentLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<OutgoingPaymentLine>
{
    public void Configure(EntityTypeBuilder<OutgoingPaymentLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Amount).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.ApInvoice)
            .WithMany()
            .HasForeignKey(l => l.ApInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
