namespace ZARI.Application.Features.Workflow.Notifications.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.Notifications.Shared;
using ZARI.Domain.Common;

public sealed class GetAllNotificationsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllNotificationsQuery, Result<List<NotificationResponse>>>
{
    public async Task<Result<List<NotificationResponse>>> HandleAsync(GetAllNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        var notifications = await dbContext.Notifications
            .Include(n => n.Reads)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success(notifications.Select(NotificationMapper.ToResponse).ToList());
    }
}
