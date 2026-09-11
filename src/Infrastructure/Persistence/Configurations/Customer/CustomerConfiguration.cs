using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : BaseModelConfig, IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.Type).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Email).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(c => c.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Owner).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.Address).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.Notes).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.MemberNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(c => c.StandingDiscountPct).HasColumnType(DefaultDecimal);

        // CISA (RA 9510 / CDA MC 2019-01) Basic Credit Data fields — see Customer.cs doc comment.
        builder.Property(c => c.Tin).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(c => c.SssOrGsisNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(c => c.Sex).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.CivilStatus).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Employer).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.EmployerPosition).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(c => c.NetIncomeLastYear).HasColumnType(DefaultDecimal);
        builder.Property(c => c.PriorResidenceHistory).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.PriorEmploymentHistory).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.HousingStatus).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.BankAccountInfo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(c => c.OtherAssetsNotes).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasOne(c => c.Branch)
            .WithMany()
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ArAccount)
            .WithMany()
            .HasForeignKey(c => c.ArAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
