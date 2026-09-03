using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PosPromoSlideConfiguration : BaseModelConfig, IEntityTypeConfiguration<PosPromoSlide>
{
    public void Configure(EntityTypeBuilder<PosPromoSlide> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(s => s.Subtitle).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(s => s.ImageUrl).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(s => s.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
    }
}
