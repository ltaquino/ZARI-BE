namespace ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;

using ZARI.Application.Abstractions.Messaging;

public sealed record CancelPendingApprovalRequestCommand(string EntityType, string EntityId) : ICommand;
