namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class CostCenter : AuditableEntity
{
    // Null means company-level, matching the FE type's `branchId?: undefined = company-level` contract.
    public string? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
}
