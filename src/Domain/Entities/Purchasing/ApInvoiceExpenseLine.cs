namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One expensed line on an EXPENSE-type ApInvoice — a direct vendor bill for a service or overhead
/// cost (utilities, professional fees, manpower/salaries, etc.) with no GRPO behind it. The user
/// picks the GL expense account to charge and types a free-text description of what it was for.
/// </summary>
public sealed class ApInvoiceExpenseLine : BaseEntity
{
    public Guid ApInvoiceId { get; set; }
    public ApInvoice ApInvoice { get; set; } = default!;
    public Guid GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }

    // Classification-only, for the Purchase Book report — see ApInvoiceLine.VatType's own comment.
    // No Item to default from here, so the caller picks it explicitly (defaults to VATABLE).
    public string VatType { get; set; } = "VATABLE";
}
