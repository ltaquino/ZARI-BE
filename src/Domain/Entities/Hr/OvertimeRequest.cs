namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Uses the shared Submit/Approve workflow, same shape as LeaveRequest — DRAFT ->
/// PENDING_APPROVAL -> APPROVED, or back to DRAFT on Reject. Unlike LeaveRequest's Approve (which
/// posts a ledger entry), Approve here has no side effect yet — payable OT hours are computed at
/// Payroll time as the lesser of this request's approved hours and what DTR actually shows (not
/// built yet, see HrModuleContext.md §3.10/§4 step 9). PlannedTimeFrom/To are the requested OT
/// window; the reconciliation against actual clock-out happens downstream, not here.
/// </summary>
public sealed class OvertimeRequest : AuditableEntity
{
    public string RequestNo { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public DateTimeOffset PlannedTimeFrom { get; set; }
    public DateTimeOffset PlannedTimeTo { get; set; }

    // "ORDINARY" | "REST_DAY" | "HOLIDAY"
    public string OtType { get; set; } = default!;
    public string? Reason { get; set; }
    public string Status { get; set; } = default!;
}
