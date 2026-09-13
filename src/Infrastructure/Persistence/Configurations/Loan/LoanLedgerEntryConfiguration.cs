using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanLedgerEntryConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanLedgerEntry>
{
    public void Configure(EntityTypeBuilder<LoanLedgerEntry> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TransactionType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.ReferenceTable).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.ReferenceId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(l => l.PrincipalIn).HasColumnType(DefaultDecimal);
        builder.Property(l => l.PrincipalOut).HasColumnType(DefaultDecimal);
        builder.Property(l => l.RunningPrincipalBalance).HasColumnType(DefaultDecimal);
        builder.Property(l => l.Description).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(l => new { l.LoanAccountId, l.SequenceNo }).IsUnique();
        builder.HasIndex(l => new { l.ReferenceTable, l.ReferenceId });

        builder.HasOne(l => l.LoanAccount)
            .WithMany()
            .HasForeignKey(l => l.LoanAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
