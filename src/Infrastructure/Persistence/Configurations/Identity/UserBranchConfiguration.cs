using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class UserBranchConfiguration : BaseModelConfig, IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.HasKey(ub => new { ub.UserId, ub.BranchId });
        builder.Property(ub => ub.BranchId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Branch>().WithMany().HasForeignKey(ub => ub.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
