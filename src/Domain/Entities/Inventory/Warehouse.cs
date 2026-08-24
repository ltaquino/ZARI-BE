namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Warehouse : AuditableEntity
{
    // References the Branch mock/system-module data, which isn't a backend
    // entity yet — kept as a plain string (not Guid/FK) until that module exists.
    public string BranchId { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string WarehouseType { get; set; } = default!;
    public string Status { get; set; } = default!;
}
