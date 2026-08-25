using Microsoft.AspNetCore.Identity;
using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : BaseModelConfig, IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.FormCode });
        builder.Property(rp => rp.FormCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne<IdentityRole>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Form>().WithMany().HasForeignKey(rp => rp.FormCode).OnDelete(DeleteBehavior.Restrict);
    }
}
