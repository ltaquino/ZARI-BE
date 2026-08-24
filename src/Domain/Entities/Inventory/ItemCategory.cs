namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class ItemCategory : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid? ParentCategoryId { get; set; }
    public ItemCategory? ParentCategory { get; set; }
}
