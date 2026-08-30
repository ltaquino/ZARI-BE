using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CustomerPaymentConfiguration : BaseModelConfig, IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.ReferenceNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(p => p.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(p => p.PaymentNo).IsUnique();

        builder.HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CashAccount)
            .WithMany()
            .HasForeignKey(p => p.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CostCenter)
            .WithMany()
            .HasForeignKey(p => p.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.CustomerPayment)
            .HasForeignKey(l => l.CustomerPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
