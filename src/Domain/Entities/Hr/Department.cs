namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data — not branch-scoped, unlike Warehouse/Customer. A department (e.g.
/// "Accounting") spans the whole cooperative; its employees may sit at any branch (see
/// Employee.BranchId). See ZARI-FE/frs/human-resource/HrModuleContext.md §3.1.
/// </summary>
public sealed class Department : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;

    // Nullable — a Department must be creatable before any Employee exists to head it (same
    // chicken-and-egg as LoanApplication -> LoanAccount, resolved the same way: create first,
    // backfill once the referenced Employee row exists).
    public Guid? HeadEmployeeId { get; set; }
    public Employee? HeadEmployee { get; set; }
}
