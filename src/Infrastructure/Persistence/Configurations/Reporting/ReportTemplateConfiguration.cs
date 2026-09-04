using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ReportTemplateConfiguration : BaseModelConfig, IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.Description).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(t => t.DatasetKey).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.PaperSize).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Orientation).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.OwnerUserId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        // Flexible, variable-shaped column/filter/sort definitions — stored as JSON text rather
        // than an EF owned collection, since the shape is designer-driven per template, not fixed.
        builder.Property(t => t.ColumnsJson).IsRequired().HasColumnType("longtext");
        builder.Property(t => t.FiltersJson).IsRequired().HasColumnType("longtext");
        builder.Property(t => t.SortJson).HasColumnType("longtext");
        builder.Property(t => t.GroupByJson).IsRequired().HasColumnType("longtext");

        builder.HasIndex(t => t.DatasetKey);
        builder.HasIndex(t => t.OwnerUserId);
    }
}
