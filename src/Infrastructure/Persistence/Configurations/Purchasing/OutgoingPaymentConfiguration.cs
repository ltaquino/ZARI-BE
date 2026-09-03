using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class OutgoingPaymentConfiguration : BaseModelConfig, IEntityTypeConfiguration<OutgoingPayment>
{
    public void Configure(EntityTypeBuilder<OutgoingPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.RefNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(p => p.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(p => p.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(p => p.PaymentNo).IsUnique();
        builder.HasIndex(p => new { p.BranchId, p.PaymentDate });
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.BankAccount)
            .WithMany()
            .HasForeignKey(p => p.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CostCenter)
            .WithMany()
            .HasForeignKey(p => p.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.OutgoingPayment)
            .HasForeignKey(l => l.OutgoingPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
