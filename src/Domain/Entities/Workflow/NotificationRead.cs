namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A per-user read receipt — normalizes the FE prototype's Notification.readBy string[] into a
/// real join row so "has user X read notification Y" is an indexed lookup, not an array scan.
/// </summary>
public sealed class NotificationRead : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public DateTimeOffset ReadAt { get; set; }
}
