using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : BaseModelConfig, IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNo)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_LASTNAME);
        builder.Property(e => e.MiddleName).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_LASTNAME);
        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_LASTNAME);
        builder.Property(e => e.Suffix).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_SUFFIXNAME);
        builder.Property(e => e.Sex).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.CivilStatus).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.Address).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.Property(e => e.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.EmploymentStatus)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.SeparationReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.Property(e => e.SssNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.PhilHealthNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.PagIbigNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(e => e.Tin).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(e => e.BasicSalary).HasColumnType(DefaultDecimal);

        builder.Property(e => e.BankAccountNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(e => e.BankName).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(e => e.BarcodeNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);
        builder.Property(e => e.RfidTagNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_100);

        builder.Property(e => e.ApplicationUserId).HasMaxLength(450); // matches Identity's own Id column length

        // MySQL treats NULL as distinct from every other NULL in a UNIQUE index (unlike SQL Server,
        // which needs a filtered/partial index to get this), so plain .IsUnique() already allows
        // any number of employees with no badge/login while still enforcing uniqueness among the
        // ones that do — no filter clause needed or supported by the Pomelo provider here.
        builder.HasIndex(e => e.EmployeeNo).IsUnique();
        builder.HasIndex(e => e.BarcodeNo).IsUnique();
        builder.HasIndex(e => e.RfidTagNo).IsUnique();
        builder.HasIndex(e => e.ApplicationUserId).IsUnique();

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Position)
            .WithMany()
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApplicationUser)
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
