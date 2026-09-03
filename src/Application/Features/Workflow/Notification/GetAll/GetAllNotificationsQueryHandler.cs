namespace ZARI.Application.Features.Workflow.Notifications.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.Notifications.Shared;
using ZARI.Domain.Common;

public sealed class GetAllNotificationsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllNotificationsQuery, Result<List<NotificationResponse>>>
{
    public async Task<Result<List<NotificationResponse>>> HandleAsync(GetAllNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("NOTIFICATIONS", FormAction.View, cancellationToken))
            return Result.Failure<List<NotificationResponse>>(Error.Forbidden("Notification.Forbidden", "You do not have permission to view notifications."));

        var notifications = await dbContext.Notifications.AsNoTracking()
            .Include(n => n.Reads)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success(notifications.Select(NotificationMapper.ToResponse).ToList());
    }
}
