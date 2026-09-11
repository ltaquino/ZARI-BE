namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Marks an ACTIVE loan account's outstanding principal as uncollectible — DRAFT -> PENDING_APPROVAL
/// -> POSTED like every other Loan document, but Approve requires elevated HQ-only authority
/// (IPermissionService.HasHqApprovalAuthorityAsync — same HQ-branch-assignment shape as
/// HasCancellationAuthorityAsync, checked against FormAction.Approve instead of Cancel), since a
/// write-off permanently removes a receivable from the books rather than just recording a document.
/// Same two-tier cancellation as LoanDisbursement/LoanRestructuring once POSTED. Unlike
/// LoanRestructuring, no arrears-settlement precondition applies here — a write-off exists precisely
/// to absorb a loan that won't be collected, arrears included.
///
/// v1 policy: direct write-off method (Dr Loan Write-off Expense / Cr Loans Receivable) rather than
/// drawing down a pre-funded allowance account, since this codebase has no separate loan-loss
/// provisioning workflow that would have built up an allowance balance to draw down. The expense
/// account is picked on the document itself (WriteOffExpenseAccountId) rather than added as a new
/// default field on LoanProduct/LoanAccount, keeping this the only new schema surface for the whole
/// feature. Only PRINCIPAL is written off (mirrors LoanRestructuring: the Loans Receivable GL
/// account only ever tracks principal via LoanLedgerEntry's running balance) — any remaining unpaid
/// interest/penalty simply stops being reported once the account leaves ACTIVE (the delinquency
/// report already filters to Status == "ACTIVE"). Amount is snapshotted at Create/Update for
/// display, but Approve always re-derives it fresh off the account's ledger running balance (never
/// trusts the possibly-stale snapshot) — same rule as LoanRestructuring.OldPrincipalBalance.
/// </summary>
public sealed class LoanWriteOff : AuditableEntity
{
    public string WriteOffNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid LoanAccountId { get; set; }
    public LoanAccount LoanAccount { get; set; } = default!;

    public DateTimeOffset WriteOffDate { get; set; }
    public decimal Amount { get; set; }

    public Guid WriteOffExpenseAccountId { get; set; }
    public GlAccount WriteOffExpenseAccount { get; set; } = default!;

    public string Reason { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
