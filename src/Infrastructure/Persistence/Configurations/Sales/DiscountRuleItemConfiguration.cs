using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class DiscountRuleItemConfiguration : BaseModelConfig, IEntityTypeConfiguration<DiscountRuleItem>
{
    public void Configure(EntityTypeBuilder<DiscountRuleItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.DiscountRuleId, i.ItemId }).IsUnique();
    }
}
