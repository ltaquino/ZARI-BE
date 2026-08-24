namespace ZARI.Application.Features.Workflow.Notifications.Shared;

using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Entities;

internal static class NotificationMapper
{
    public static NotificationResponse ToResponse(Notification notification) => new(
        notification.Id,
        notification.EntityType,
        notification.EntityId,
        notification.BranchId,
        notification.Type,
        notification.Category,
        notification.Message,
        notification.ActorUserId,
        notification.Reads.Select(r => r.UserId).ToList(),
        notification.CreatedAt);
}
