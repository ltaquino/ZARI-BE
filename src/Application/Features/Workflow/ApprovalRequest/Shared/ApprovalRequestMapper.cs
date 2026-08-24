namespace ZARI.Application.Features.Workflow.ApprovalRequests.Shared;

using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Domain.Entities;

internal static class ApprovalRequestMapper
{
    public static ApprovalRequestResponse ToResponse(ApprovalRequest request) => new(
        request.Id,
        request.EntityType,
        request.EntityId,
        request.BranchId,
        request.RequestedBy,
        request.RequestedAt,
        request.Status,
        request.RequestType,
        request.Reason,
        request.Actions
            .OrderBy(a => a.ActionAt)
            .Select(a => new ApprovalActionResponse(a.Id, a.ApprovalRequestId, a.ApproverUserId, a.Action, a.ActionAt, a.Comments, a.CreatedAt))
            .ToList(),
        request.CreatedAt);
}
