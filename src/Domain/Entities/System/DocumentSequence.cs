namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class DocumentSequence : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string DocType { get; set; } = default!;
    public string Prefix { get; set; } = default!;
    public int NextNumber { get; set; }
    public int PaddingLength { get; set; }
}
