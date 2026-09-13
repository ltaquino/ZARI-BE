namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Uses the shared Submit/Approve workflow (Application/Features/Workflow/ApprovalRequest*)
/// verbatim — DRAFT -> PENDING_APPROVAL -> APPROVED, or back to DRAFT on Reject, same shape as
/// PurchaseRequest. On Approve, posts a USAGE row to LeaveBalanceEntry via
/// LeaveBalanceLedgerService. See HrModuleContext.md §3.9.
/// </summary>
public sealed class LeaveRequest : AuditableEntity
{
    public string RequestNo { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = default!;
    public DateTimeOffset DateFrom { get; set; }
    public DateTimeOffset DateTo { get; set; }

    // Derived at Create time — count of scheduled work days in [DateFrom, DateTo] per the
    // employee's effective WorkSchedule — never recomputed afterward (a later schedule change
    // must not retroactively alter an already-submitted request's day count).
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Status { get; set; } = default!;
}
