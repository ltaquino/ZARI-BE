namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A guarantor on a LoanApplication — a child record, not its own Form (§4.3 of the context doc).
/// CoMakerCustomerId is an optional link when the co-maker happens to be an existing member; Name/
/// ContactNo are always stored directly (a denormalized snapshot, same idiom a transaction line
/// snapshots ItemCode/ItemName) so a non-member guarantor never needs a fabricated Customer row —
/// mirrors Supplier.Owner's existing free-text-person precedent.
/// </summary>
public sealed class LoanCoMaker : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = default!;

    public Guid? CoMakerCustomerId { get; set; }
    public Customer? CoMakerCustomer { get; set; }

    public string Name { get; set; } = default!;
    public string? ContactNo { get; set; }
}
