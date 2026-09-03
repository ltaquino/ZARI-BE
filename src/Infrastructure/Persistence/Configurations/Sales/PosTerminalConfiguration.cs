using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PosTerminalConfiguration : BaseModelConfig, IEntityTypeConfiguration<PosTerminal>
{
    public void Configure(EntityTypeBuilder<PosTerminal> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.MachineIdentificationNumber).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.MachineSerialNumber).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.PosPermitNumber).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(t => new { t.BranchId, t.Code }).IsUnique();

        builder.HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
