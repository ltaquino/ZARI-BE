using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class LoanWriteOffConfiguration : BaseModelConfig, IEntityTypeConfiguration<LoanWriteOff>
{
    public void Configure(EntityTypeBuilder<LoanWriteOff> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WriteOffNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(w => w.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(w => w.Amount).HasColumnType(DefaultDecimal);
        builder.Property(w => w.Reason).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(w => w.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(w => w.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(w => w.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(w => w.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(w => w.WriteOffNo).IsUnique();
        builder.HasIndex(w => new { w.BranchId, w.WriteOffDate });
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => w.LoanAccountId);

        builder.HasOne(w => w.Branch).WithMany().HasForeignKey(w => w.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.LoanAccount).WithMany().HasForeignKey(w => w.LoanAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.WriteOffExpenseAccount).WithMany().HasForeignKey(w => w.WriteOffExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
