using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : BaseModelConfig, IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(v => v.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(v => v.PlateNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(v => v.Description).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(v => v.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(v => new { v.BranchId, v.Code }).IsUnique();
        builder.HasIndex(v => v.PlateNo).IsUnique();
        builder.HasIndex(v => v.WarehouseId).IsUnique();
        builder.HasIndex(v => v.PosTerminalId).IsUnique();

        builder.HasOne(v => v.Branch).WithMany().HasForeignKey(v => v.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Warehouse).WithMany().HasForeignKey(v => v.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.PosTerminal).WithMany().HasForeignKey(v => v.PosTerminalId).OnDelete(DeleteBehavior.Restrict);
    }
}
