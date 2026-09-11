using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CustomerCreditRecordConfiguration : BaseModelConfig, IEntityTypeConfiguration<CustomerCreditRecord>
{
    public void Configure(EntityTypeBuilder<CustomerCreditRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RecordType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Description).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.Amount).HasColumnType(DefaultDecimal);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.RecordType);

        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
