namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A posted (or reversed) GL journal — immutable once created except for the Status flip a
/// reversal applies to the original. Always posted by another module's approval engine (currently
/// Inventory), never created directly by a user form — see the FE type's original doc comment.
/// </summary>
public sealed class GlJournal : AuditableEntity
{
    public string JournalNo { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public DateTimeOffset JournalDate { get; set; }
    public string SourceModule { get; set; } = default!;
    public string SourceReferenceTable { get; set; } = default!;
    public string SourceReferenceId { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;

    public Guid? ReversalOfJournalId { get; set; }
    public GlJournal? ReversalOfJournal { get; set; }

    public List<GlJournalLine> Lines { get; set; } = [];
}
