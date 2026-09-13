namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data (like Department — not branch-scoped). PayGradeMin/Max, when set, are
/// enforced against Employee.BasicSalary on Create/Update (confirmed with the cooperative that pay
/// grades are actually used) — either bound left null skips that check, so a position with no
/// configured grade imposes no restriction. See HrModuleContext.md §3.2.
/// </summary>
public sealed class Position : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal? PayGradeMin { get; set; }
    public decimal? PayGradeMax { get; set; }
}
