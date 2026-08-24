namespace ZARI.Application.Features.Workflow.ApprovalRequests.Decide;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Domain.Common;

public sealed record DecideApprovalRequestCommand(Guid Id, string ApproverUserId, string Action, string? Comments) : ICommand<Result<ApprovalRequestResponse>>;
