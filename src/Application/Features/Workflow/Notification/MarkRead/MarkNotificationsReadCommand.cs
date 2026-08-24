namespace ZARI.Application.Features.Workflow.Notifications.MarkRead;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record MarkNotificationsReadCommand(List<Guid> Ids, string UserId) : ICommand;
