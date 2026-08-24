namespace ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllApprovalRequestsQuery : IQuery<Result<List<ApprovalRequestResponse>>>;

public sealed record ApprovalActionResponse(
    Guid Id,
    Guid ApprovalRequestId,
    string ApproverUserId,
    string Action,
    DateTimeOffset ActionAt,
    string? Comments,
    DateTimeOffset CreatedAt);

public sealed record ApprovalRequestResponse(
    Guid Id,
    string EntityType,
    string EntityId,
    string BranchId,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    string Status,
    string RequestType,
    string? Reason,
    List<ApprovalActionResponse> Actions,
    DateTimeOffset CreatedAt);
