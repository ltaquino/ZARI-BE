namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class FiscalYear : AuditableEntity
{
    public string YearName { get; set; } = default!;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string Status { get; set; } = default!;
}
