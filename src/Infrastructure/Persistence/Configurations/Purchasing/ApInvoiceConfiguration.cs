using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ApInvoiceConfiguration : BaseModelConfig, IEntityTypeConfiguration<ApInvoice>
{
    public void Configure(EntityTypeBuilder<ApInvoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.InvoiceType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.SupplierInvoiceNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(i => i.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(i => i.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(i => i.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(i => i.InvoiceNo).IsUnique();

        // Double-billing protection: the same vendor invoice number can't be entered twice for the
        // same supplier.
        builder.HasIndex(i => new { i.SupplierId, i.SupplierInvoiceNo }).IsUnique();

        builder.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Supplier)
            .WithMany()
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.GoodsReceiptPo)
            .WithMany()
            .HasForeignKey(i => i.GoodsReceiptPoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.ApInvoice)
            .HasForeignKey(l => l.ApInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.ExpenseLines)
            .WithOne(l => l.ApInvoice)
            .HasForeignKey(l => l.ApInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
