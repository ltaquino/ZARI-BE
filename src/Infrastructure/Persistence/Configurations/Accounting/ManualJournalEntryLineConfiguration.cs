using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ManualJournalEntryLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<ManualJournalEntryLine>
{
    public void Configure(EntityTypeBuilder<ManualJournalEntryLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Memo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(l => l.DebitAmount).HasColumnType(DefaultDecimal);
        builder.Property(l => l.CreditAmount).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.GlAccount)
            .WithMany()
            .HasForeignKey(l => l.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CostCenter)
            .WithMany()
            .HasForeignKey(l => l.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
