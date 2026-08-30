using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ItemConfiguration : BaseModelConfig, IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_ITEMCODE);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(i => i.Description)
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.Property(i => i.ItemType)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(i => i.CostingMethod)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(i => i.SalesAccountId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.PurchaseAccountId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.InventoryAccountId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.CogsAccountId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(i => i.VatType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT).HasDefaultValue("VATABLE");

        builder.HasIndex(i => i.Code).IsUnique();

        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.BaseUom)
            .WithMany()
            .HasForeignKey(i => i.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
