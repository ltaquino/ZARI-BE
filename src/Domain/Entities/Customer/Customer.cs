namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Customer : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string Status { get; set; } = default!;

    // The salesperson/account owner's display name — plain string, not a User FK, since Users
    // aren't a backend entity yet either.
    public string Owner { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Notes { get; set; }
}
