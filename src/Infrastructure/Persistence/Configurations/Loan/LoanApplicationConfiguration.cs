using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanApplicationConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApplicationNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.RequestedPrincipal).HasColumnType(DefaultDecimal);
        builder.Property(a => a.Purpose).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(a => a.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(a => a.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(a => a.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(a => a.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(a => a.ApplicationNo).IsUnique();
        builder.HasIndex(a => new { a.BranchId, a.ApplicationDate });
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.LoanProduct).WithMany().HasForeignKey(a => a.LoanProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Collaterals).WithOne(c => c.LoanApplication).HasForeignKey(c => c.LoanApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.CoMakers).WithOne(c => c.LoanApplication).HasForeignKey(c => c.LoanApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}
