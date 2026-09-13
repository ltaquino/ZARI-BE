namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Branch-scoped (unlike Department/Position, which are global) — mirrors Customer.BranchId.
/// See ZARI-FE/frs/human-resource/HrModuleContext.md §3.3.
/// </summary>
public sealed class Employee : AuditableEntity
{
    // Plain, manually-entered/admin-assigned code — matches this codebase's master-data
    // convention (Customer/Supplier have no DocumentSequence-backed auto-numbering either;
    // GetNextDocumentNumberCommand is reserved for transactional workflow documents).
    public string EmployeeNo { get; set; } = default!;

    public string FirstName { get; set; } = default!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = default!;
    public string? Suffix { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? CivilStatus { get; set; }
    public string? Address { get; set; }

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid PositionId { get; set; }
    public Position Position { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public DateTimeOffset DateHired { get; set; }

    // Simple on/off catalog toggle, separate from the richer EmploymentStatus lifecycle below —
    // same coexistence as Branch.Status alongside LoanAccount.Status elsewhere in this codebase.
    public string Status { get; set; } = default!;

    // Plain string, not an enum type (this codebase's universal convention):
    // PROBATIONARY -> REGULAR, terminal RESIGNED/TERMINATED/RETIRED reachable from either.
    public string EmploymentStatus { get; set; } = default!;
    public DateTimeOffset? RegularizationDate { get; set; }
    public DateTimeOffset? SeparationDate { get; set; }
    public string? SeparationReason { get; set; }

    // Government-mandated numbers for payroll withholding — distinct from Customer's own
    // SssOrGsisNo/Tin, which serve CISA/credit-data reporting on loan applicants, not payroll.
    public string? SssNo { get; set; }
    public string? PhilHealthNo { get; set; }
    public string? PagIbigNo { get; set; }
    public string? Tin { get; set; }

    // Monthly rate — semi-monthly/daily rates are always derived at payroll time, never cached.
    public decimal BasicSalary { get; set; }

    public string? BankAccountNo { get; set; }
    public string? BankName { get; set; }

    // Badge identifiers scanned at the DTR kiosk (HrModuleContext.md §3.6) — both independent and
    // nullable (a cooperative may issue one medium, the other, or neither). Each unique when
    // non-null (enforced via a filtered index in EmployeeConfiguration).
    public string? BarcodeNo { get; set; }
    public string? RfidTagNo { get; set; }

    // Nullable — not every employee has (or needs) a ZARI login; not every ApplicationUser is
    // necessarily an Employee (e.g. a contracted IT consultant). Unique when non-null. Identity
    // PKs are string, not Guid, hence the type here.
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
}
