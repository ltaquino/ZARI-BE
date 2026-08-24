namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One decision (Approve/Reject) recorded against an ApprovalRequest — the audit trail of who
/// decided what and when. Always created alongside a status transition on its parent; never
/// edited afterward.
/// </summary>
public sealed class ApprovalAction : AuditableEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = default!;

    // References the Users mock/system-module data — plain string, not FK, same as ApprovalRequest.RequestedBy.
    public string ApproverUserId { get; set; } = default!;

    public string Action { get; set; } = default!;
    public DateTimeOffset ActionAt { get; set; }
    public string? Comments { get; set; }
}
