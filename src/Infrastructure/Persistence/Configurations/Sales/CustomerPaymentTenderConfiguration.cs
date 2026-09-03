using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CustomerPaymentTenderConfiguration : BaseModelConfig, IEntityTypeConfiguration<CustomerPaymentTender>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentTender> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasColumnType(DefaultDecimal);
        builder.Property(t => t.ReferenceNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.BankOrPartnerName).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.HasOne(t => t.PaymentMethod)
            .WithMany()
            .HasForeignKey(t => t.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
