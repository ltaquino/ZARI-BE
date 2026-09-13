using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class PayrollLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<PayrollLine>
{
    public void Configure(EntityTypeBuilder<PayrollLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BasicPay).HasColumnType(DefaultDecimal);
        builder.Property(l => l.OvertimePay).HasColumnType(DefaultDecimal);
        builder.Property(l => l.AllowanceTotal).HasColumnType(DefaultDecimal);
        builder.Property(l => l.GrossPay).HasColumnType(DefaultDecimal);
        builder.Property(l => l.SssEmployeeShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.SssEmployerShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.PhilHealthEmployeeShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.PhilHealthEmployerShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.PagIbigEmployeeShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.PagIbigEmployerShare).HasColumnType(DefaultDecimal);
        builder.Property(l => l.WithholdingTax).HasColumnType(DefaultDecimal);
        builder.Property(l => l.DeductionTotal).HasColumnType(DefaultDecimal);
        builder.Property(l => l.NetPay).HasColumnType(DefaultDecimal);

        builder.HasIndex(l => new { l.PayrollRunId, l.EmployeeId }).IsUnique();

        builder.HasOne(l => l.Employee).WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
