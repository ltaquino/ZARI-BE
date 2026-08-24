namespace ZARI.Application.Features.Workflow.ApprovalRequests.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitForApprovalCommand(
    string EntityType,
    string EntityId,
    string BranchId,
    string RequestedBy,
    string? RequestType,
    string? Reason) : ICommand<Result<ApprovalRequestResponse>>;
