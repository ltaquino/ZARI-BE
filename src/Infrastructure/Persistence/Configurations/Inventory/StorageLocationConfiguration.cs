using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StorageLocationConfiguration : BaseModelConfig, IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Zone).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.Aisle).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.Rack).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.BinCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(l => l.Warehouse)
            .WithMany()
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
