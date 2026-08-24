namespace ZARI.Domain.Entities;

// Every other entity in this codebase uses a Guid Id (via BaseEntity), but every module that
// already references a branch — Warehouse, Customer, DocumentSequence, and every inventory
// transaction document — stores BranchId as a plain string slug ("br-hq", "br-north", ...),
// matching the FE mock's ids. Giving Branch a string Id (instead of the usual Guid) lets every
// one of those pre-existing columns become a real foreign key with no data migration: the values
// already stored there match exactly. Not AuditableEntity either — the FE's Branch type has no
// createdAt/createdBy fields.
public sealed class Branch
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Status { get; set; } = default!;

    // The HQ branch — an Admin assigned here is the only one who may approve cancellation of a
    // posted document (see the FE type's doc comment). At most one branch has this set.
    public bool IsHeadOffice { get; set; }
}
