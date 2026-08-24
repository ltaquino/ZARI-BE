using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ItemBranchSettingConfiguration : BaseModelConfig, IEntityTypeConfiguration<ItemBranchSetting>
{
    public void Configure(EntityTypeBuilder<ItemBranchSetting> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(s => s.ReorderPoint).HasColumnType(DefaultDecimal);
        builder.Property(s => s.MinStock).HasColumnType(DefaultDecimal);
        builder.Property(s => s.MaxStock).HasColumnType(DefaultDecimal);

        builder.HasIndex(s => new { s.ItemId, s.BranchId }).IsUnique();

        builder.HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DefaultWarehouse)
            .WithMany()
            .HasForeignKey(s => s.DefaultWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
