using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class UserFormPermissionOverrideConfiguration : BaseModelConfig, IEntityTypeConfiguration<UserFormPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserFormPermissionOverride> builder)
    {
        builder.HasKey(o => new { o.UserId, o.FormCode });
        builder.Property(o => o.FormCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Form>().WithMany().HasForeignKey(o => o.FormCode).OnDelete(DeleteBehavior.Restrict);
    }
}
