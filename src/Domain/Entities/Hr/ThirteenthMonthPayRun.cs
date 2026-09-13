namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Per-branch, per-calendar-year 13th month pay run (PD 851). Lifecycle: DRAFT --(Compute,
/// repeatable)--> DRAFT -> (Submit) -> PENDING_APPROVAL -> (Approve) -> POSTED, or (Reject) -> back
/// to DRAFT — the same shape as PayrollRun. Compute sums BasicPay across every POSTED PayrollLine
/// for the employee within Year, divided by 12 — a formula that stays correct regardless of pay
/// cadence, since it only sums what was actually paid. v1 explicitly does not withhold tax on the
/// 13th-month amount itself (real BIR rule: exempt up to a threshold, taxable above) — a known
/// simplification, not an oversight.
/// </summary>
public sealed class ThirteenthMonthPayRun : AuditableEntity
{
    public string RunNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public int Year { get; set; }
    public DateTimeOffset PayDate { get; set; }
    public string Status { get; set; } = default!;
    public ICollection<ThirteenthMonthPayLine> Lines { get; set; } = new List<ThirteenthMonthPayLine>();
}
