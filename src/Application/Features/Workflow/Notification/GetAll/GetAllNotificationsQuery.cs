namespace ZARI.Application.Features.Workflow.Notifications.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllNotificationsQuery : IQuery<Result<List<NotificationResponse>>>;

public sealed record NotificationResponse(
    Guid Id,
    string EntityType,
    string EntityId,
    string BranchId,
    string Type,
    string Category,
    string Message,
    string? ActorUserId,
    List<string> ReadBy,
    DateTimeOffset CreatedAt);
