namespace ZARI.Application.Features.Workflow.Notifications.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateNotificationCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>>
{
    public async Task<Result<NotificationResponse>> HandleAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            EntityType = command.EntityType,
            EntityId = command.EntityId,
            BranchId = command.BranchId,
            Type = command.Type,
            Category = command.Category,
            Message = command.Message,
            ActorUserId = command.ActorUserId
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(NotificationMapper.ToResponse(notification));
    }
}
