namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Global master data (like Department — not branch-scoped). PayGradeMin/Max are a non-enforced
/// suggestion only (same idiom as ItemBranchSetting.MarkupPct suggesting SellingPrice), never
/// validated against Employee.BasicSalary. See HrModuleContext.md §3.2.
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
