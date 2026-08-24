namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class TodoItem : AuditableEntity
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
    }
}
