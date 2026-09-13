namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Append-only ledger — same idiom as LoanLedgerEntry, never a single mutable balance column.
/// Written only via LeaveBalanceLedgerService (either a manual HR-posted ACCRUAL/CARRYOVER/
/// ADJUSTMENT, or a USAGE row posted automatically when a LeaveRequest is approved) — never
/// directly editable/deletable once posted. See HrModuleContext.md §3.8.
/// </summary>
public sealed class LeaveBalanceEntry : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = default!;
    public DateTimeOffset Date { get; set; }

    // "ACCRUAL" | "USAGE" | "CARRYOVER" | "ADJUSTMENT"
    public string Type { get; set; } = default!;

    // Positive for ACCRUAL/CARRYOVER, negative for USAGE; ADJUSTMENT can be either sign.
    public decimal Days { get; set; }
    public decimal RunningBalance { get; set; }
    public string? ReferenceTable { get; set; }
    public string? ReferenceId { get; set; }
}
