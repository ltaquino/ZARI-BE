using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GlJournalLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<GlJournalLine>
{
    public void Configure(EntityTypeBuilder<GlJournalLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.DebitAmount).HasColumnType(DefaultDecimal);
        builder.Property(l => l.CreditAmount).HasColumnType(DefaultDecimal);
        builder.Property(l => l.Memo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CostCenter)
            .WithMany()
            .HasForeignKey(l => l.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
