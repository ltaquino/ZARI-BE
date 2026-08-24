namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class CostCenter : AuditableEntity
{
    // References the Branch mock/system-module data, which isn't a backend entity yet —
    // kept as a plain string (not Guid/FK), matching StockReservation.BranchId. Null means
    // company-level, matching the FE type's `branchId?: undefined = company-level` contract.
    public string? BranchId { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
}
