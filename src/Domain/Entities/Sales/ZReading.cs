namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Permanent audit record of one BIR Z-Reading close (SalesModuleContext.md §3.7). The cutoff is
/// defined by BIR-OR number range, not calendar day — each Z-Reading closes every POSTED
/// SalesInvoice for this branch since the previous Z-Reading's LastOrNumber, up to the highest
/// BIR-OR number assigned at the moment this reading is run. That makes "must refuse to run if a
/// prior period was never Z-read" true by construction — there is no calendar boundary to skip,
/// the cutoff is always contiguous from wherever the last one left off.
///
/// No workflow status, no ApprovalRequest, no cancellation — once run, a Z-Reading is permanent,
/// mirroring how a BIR-OR number, once assigned, is never reassigned even on invoice cancellation.
///
/// AuditableEntity's CreatedAt/CreatedBy double as this reading's RunAt/RunBy — not duplicated
/// here as separate fields.
/// </summary>
public sealed class ZReading : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    /// <summary>The Branch.ZCounter value this reading produced (post-increment) — the number printed on the Z-Reading tape.</summary>
    public int ZCounterValue { get; set; }

    /// <summary>Null only when this period had zero POSTED invoices — a legitimate zero-activity close.</summary>
    public string? FirstOrNumber { get; set; }
    public string? LastOrNumber { get; set; }

    /// <summary>Display-only period bounds: PeriodStart is derived from the previous ZReading's PeriodEnd
    /// (or the earliest InvoiceDate in the closed set, for a branch's first-ever reading); PeriodEnd is
    /// this reading's own run time.</summary>
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }

    public int InvoiceCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal VatableSales { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatExemptSales { get; set; }
    public decimal ZeroRatedSales { get; set; }

    /// <summary>VatableSales + VatExemptSales + ZeroRatedSales — reconciles to the sum of totalAr posted across the closed invoices.</summary>
    public decimal NetSales { get; set; }
}
