using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class BankAccountConfiguration : BaseModelConfig, IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(b => b.AccountName)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(b => b.AccountNumber)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(b => b.BankName)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(b => b.CurrencyId)
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(b => b.Branch)
            .WithMany()
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.GlAccount)
            .WithMany()
            .HasForeignKey(b => b.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Currency)
            .WithMany()
            .HasForeignKey(b => b.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
