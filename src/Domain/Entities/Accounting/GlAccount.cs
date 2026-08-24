namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GlAccount : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string AccountType { get; set; } = default!;
    public string NormalBalance { get; set; } = default!;

    public Guid? ParentAccountId { get; set; }
    public GlAccount? ParentAccount { get; set; }

    public string Status { get; set; } = default!;
}
