namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A member's request for a loan — DRAFT/PENDING_APPROVAL/APPROVED workflow only, no stock/GL
/// side effects at all (same shape as PurchaseRequest — see ApproveLoanApplicationCommandHandler).
/// An approved LoanApplication doesn't automatically create a LoanAccount; creating one is a
/// separate manual step that may reference an approved application, the same PR->PO relationship
/// pattern (ZARI-FE/frs/loan/LoanModuleContext.md §4.2).
/// </summary>
public sealed class LoanApplication : AuditableEntity
{
    public string ApplicationNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid LoanProductId { get; set; }
    public LoanProduct LoanProduct { get; set; } = default!;

    public DateTimeOffset ApplicationDate { get; set; }
    public decimal RequestedPrincipal { get; set; }
    public int RequestedTermMonths { get; set; }
    public string? Purpose { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public List<LoanCollateral> Collaterals { get; set; } = [];
    public List<LoanCoMaker> CoMakers { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
