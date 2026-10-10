using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class DeliveryRouteConfiguration : BaseModelConfig, IEntityTypeConfiguration<DeliveryRoute>
{
    public void Configure(EntityTypeBuilder<DeliveryRoute> builder)
    {
        builder.ToTable("DeliveryRoutes");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.Description).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(r => new { r.BranchId, r.Code }).IsUnique();

        builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DefaultVehicle).WithMany().HasForeignKey(r => r.DefaultVehicleId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.HasMany(r => r.Stops).WithOne(s => s.Route).HasForeignKey(s => s.RouteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DeliveryRouteStopConfiguration : BaseModelConfig, IEntityTypeConfiguration<DeliveryRouteStop>
{
    public void Configure(EntityTypeBuilder<DeliveryRouteStop> builder)
    {
        builder.ToTable("DeliveryRouteStops");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Notes).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        // A customer appears once per route; visiting order is unique within the route.
        builder.HasIndex(s => new { s.RouteId, s.CustomerId }).IsUnique();
        builder.HasIndex(s => new { s.RouteId, s.Sequence }).IsUnique();

        builder.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
