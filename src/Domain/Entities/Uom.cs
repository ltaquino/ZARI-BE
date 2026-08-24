namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Uom : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
