using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ApprovalRequestConfiguration : BaseModelConfig, IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EntityType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.EntityId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.RequestedBy).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.RequestType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Reason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        // getLatestApprovalRequestSync / getApprovalHistoryForEntitySync both key off this pair.
        builder.HasIndex(r => new { r.EntityType, r.EntityId });

        builder.HasMany(r => r.Actions)
            .WithOne(a => a.ApprovalRequest)
            .HasForeignKey(a => a.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
