namespace ZARI.Application.Features.Workflow.Notifications.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

public sealed record CreateNotificationCommand(
    string EntityType,
    string EntityId,
    string BranchId,
    string Type,
    string Category,
    string Message,
    string? ActorUserId) : ICommand<Result<NotificationResponse>>;
