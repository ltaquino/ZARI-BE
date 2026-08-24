using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CostCenterConfiguration : BaseModelConfig, IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BranchId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasOne(c => c.Branch)
            .WithMany()
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
