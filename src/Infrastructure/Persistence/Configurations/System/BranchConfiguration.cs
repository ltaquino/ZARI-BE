using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : BaseModelConfig, IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT).ValueGeneratedNever();

        builder.Property(b => b.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(b => b.Code).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(b => b.City).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(b => b.Address).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(b => b.Phone).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(b => b.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(b => b.BirBranchCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(b => b.PosPermitNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(b => b.MachineIdentificationNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(b => b.MachineSerialNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasIndex(b => b.Code).IsUnique();
    }
}
