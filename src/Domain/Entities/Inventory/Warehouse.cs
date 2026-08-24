namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Warehouse : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string WarehouseType { get; set; } = default!;
    public string Status { get; set; } = default!;
}
