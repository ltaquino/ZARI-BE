namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// "Negative credit information" per RA 9510 (Credit Information System Act) / CDA MC 2019-01 —
/// defaults, foreclosures, adverse court judgments, bankruptcy/insolvency, bounced-check inclusion,
/// and pending litigation affecting a member's creditworthiness, entered manually since this is
/// information about the member's broader credit history, not derivable from ZARI's own Loan
/// documents (a written-off ZARI loan already shows up via LoanWriteOff; this is for everything
/// else CIC's Basic Credit Data profile expects — see Features/Loan/Reports/CisaCreditData). Plain
/// CRUD, no workflow — this is compliance record-keeping, not a money-moving document.
/// </summary>
public sealed class CustomerCreditRecord : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    // "DEFAULT" | "FORECLOSURE" | "ADVERSE_JUDGMENT" | "BANKRUPTCY" | "BOUNCED_CHECK" |
    // "PENDING_LITIGATION" | "OTHER" — same enum-shaped-string convention as every other
    // status/type field in this codebase.
    public string RecordType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTimeOffset RecordDate { get; set; }
    public decimal? Amount { get; set; }
    public string? Remarks { get; set; }
}
