using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ManualJournalEntryConfiguration : BaseModelConfig, IEntityTypeConfiguration<ManualJournalEntry>
{
    public void Configure(EntityTypeBuilder<ManualJournalEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.Remarks).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(e => e.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(e => e.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(e => e.EntryNo).IsUnique();
        builder.HasIndex(e => new { e.BranchId, e.EntryDate });
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lines)
            .WithOne(l => l.ManualJournalEntry)
            .HasForeignKey(l => l.ManualJournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
