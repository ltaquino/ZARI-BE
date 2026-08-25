using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class FormConfiguration : BaseModelConfig, IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.HasKey(f => f.Code);
        builder.Property(f => f.Code).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT).ValueGeneratedNever();

        builder.Property(f => f.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(f => f.Module).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
    }
}
