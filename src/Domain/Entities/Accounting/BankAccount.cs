namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class BankAccount : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public string AccountName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string BankName { get; set; } = default!;

    public Guid GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = default!;

    public string? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
}
