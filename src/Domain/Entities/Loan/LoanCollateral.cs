namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A child record on a LoanApplication — not a top-level document/Form of its own, the same way
/// a PurchaseOrderLine isn't (ZARI-FE/frs/loan/LoanModuleContext.md §4.3).
/// </summary>
public sealed class LoanCollateral : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = default!;

    public string Description { get; set; } = default!;
    // e.g. "REAL_ESTATE" / "VEHICLE" / "CHATTEL" / "DEPOSIT_HOLD_OUT" — enum-shaped string, same
    // convention as every other type/status field in this codebase.
    public string CollateralType { get; set; } = default!;
    public decimal EstimatedValue { get; set; }
    public string? DocumentRef { get; set; }
}
