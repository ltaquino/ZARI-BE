namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Loan master data — the terms a LoanAccount snapshots at disbursement time (see
/// ZARI-FE/frs/loan/LoanModuleContext.md §4.4: rate/term/frequency are copied onto the account
/// once and never read live off this row again, so a later product change can't corrupt a
/// schedule already amortizing). "DIMINISHING" is the only InterestMethod actually implemented —
/// "FLAT_ADD_ON" is reserved for a future loan type, not built (§3.5).
/// </summary>
public sealed class LoanProduct : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string InterestMethod { get; set; } = default!;
    public decimal AnnualInterestRatePct { get; set; }

    public decimal MinPrincipal { get; set; }
    public decimal MaxPrincipal { get; set; }
    public int MinTermMonths { get; set; }
    public int MaxTermMonths { get; set; }

    // "MONTHLY" | "WEEKLY" | "SEMI_MONTHLY" — same enum-shaped-string convention as every other
    // status/type field in this codebase.
    public string RepaymentFrequency { get; set; } = default!;

    public int GracePeriodDays { get; set; }
    // Exact computation rule (flat %/day? compounding?) is an open question — see §6 of the
    // context doc. This is just the rate the eventual penalty engine will read.
    public decimal PenaltyRatePct { get; set; }

    public bool RequiresCollateral { get; set; }
    public bool RequiresCoMaker { get; set; }

    // Default GL accounts for this product — mirrors Item.SalesAccountId/CogsAccountId's
    // per-master-data GL account fields.
    public Guid? LoanReceivableAccountId { get; set; }
    public GlAccount? LoanReceivableAccount { get; set; }
    public Guid? InterestIncomeAccountId { get; set; }
    public GlAccount? InterestIncomeAccount { get; set; }
    public Guid? PenaltyIncomeAccountId { get; set; }
    public GlAccount? PenaltyIncomeAccount { get; set; }

    public string Status { get; set; } = default!;

    // CIC CSDF "CI" record's Contract Type domain code (e.g. "12"=Personal Loan, "18"=Unsecured
    // Loan — ZARI-FE/frs/loan-cic/LoanCicContext.md §4.2) for loans of this product. A cooperative
    // policy call per product, not derivable from anything else on this entity — left null until
    // set, and defaulted to a generic code at export time (see CicDomainCodes.ContractTypeCode).
    public string? CicContractTypeCode { get; set; }
}
