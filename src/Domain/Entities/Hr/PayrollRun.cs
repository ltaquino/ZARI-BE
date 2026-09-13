namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Per-branch payroll run header. Lifecycle: DRAFT --(Compute, repeatable)--> DRAFT -> (Submit) ->
/// PENDING_APPROVAL -> (Approve) -> POSTED, or (Reject) -> back to DRAFT. Compute is the one step
/// with no shared-workflow precedent elsewhere in this codebase — it (re)generates every Lines row
/// from Employee/DTR/OvertimeRequest/EmployeeAllowance/EmployeeDeduction/government-bracket data,
/// safely re-runnable while still DRAFT (unlike LoanDisbursement, which generates its schedule and
/// posts GL in one irreversible step). See HrModuleContext.md §3.13.
/// </summary>
public sealed class PayrollRun : AuditableEntity
{
    public string PayrollRunNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public DateTimeOffset PayPeriodStart { get; set; }
    public DateTimeOffset PayPeriodEnd { get; set; }
    public DateTimeOffset PayDate { get; set; }
    public string Status { get; set; } = default!;
    public ICollection<PayrollLine> Lines { get; set; } = new List<PayrollLine>();
}
