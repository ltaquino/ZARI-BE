using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GlJournalConfiguration : BaseModelConfig, IEntityTypeConfiguration<GlJournal>
{
    public void Configure(EntityTypeBuilder<GlJournal> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.JournalNo)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(j => j.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(j => j.SourceModule)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(j => j.SourceReferenceTable)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(j => j.SourceReferenceId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(j => j.Description).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        // The lookup pattern reverseJournalsFor(sourceReferenceTable, sourceReferenceId) runs on
        // every document cancellation-approval — this index is what keeps that a point lookup.
        builder.HasIndex(j => new { j.SourceReferenceTable, j.SourceReferenceId });

        builder.HasOne(j => j.ReversalOfJournal)
            .WithMany()
            .HasForeignKey(j => j.ReversalOfJournalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Branch)
            .WithMany()
            .HasForeignKey(j => j.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.Lines)
            .WithOne(l => l.GlJournal)
            .HasForeignKey(l => l.GlJournalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
