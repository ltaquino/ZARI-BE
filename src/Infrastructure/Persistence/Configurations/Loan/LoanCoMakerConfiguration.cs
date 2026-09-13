using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanCoMakerConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanCoMaker>
{
    public void Configure(EntityTypeBuilder<LoanCoMaker> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.ContactNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);

        builder.HasOne(c => c.CoMakerCustomer)
            .WithMany()
            .HasForeignKey(c => c.CoMakerCustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
