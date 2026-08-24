namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StorageLocation : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public string? Zone { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? BinCode { get; set; }
}
