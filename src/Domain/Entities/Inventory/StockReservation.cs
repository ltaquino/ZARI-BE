namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Earmarks qty at a warehouse so it can't be issued out from under whatever it's held for.
/// Warehouse-level, not batch-specific — see the FE type comment (types/index.ts) for the full
/// rationale. Created ACTIVE; transitions to RELEASED (or, once wired up, CONSUMED) — never
/// edited field-by-field after creation, so there is deliberately no generic Update endpoint.
/// </summary>
public sealed class StockReservation : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    // References the Branch mock/system-module data, which isn't a backend
    // entity yet — kept as a plain string (not Guid/FK) until that module exists.
    public string BranchId { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public decimal QtyReserved { get; set; }
    public DateTimeOffset ReservedDate { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public string? ReferenceNote { get; set; }
    public string Status { get; set; } = default!;
    public string? ReleasedBy { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}
