namespace ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllApprovalRequestsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllApprovalRequestsQuery, Result<List<ApprovalRequestResponse>>>
{
    public async Task<Result<List<ApprovalRequestResponse>>> HandleAsync(GetAllApprovalRequestsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("APPROVAL_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<List<ApprovalRequestResponse>>(Error.Forbidden("ApprovalRequest.Forbidden", "You do not have permission to view approval requests."));

        var requests = await dbContext.ApprovalRequests.AsNoTracking()
            .Include(r => r.Actions)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

        return Result.Success(requests.Select(ApprovalRequestMapper.ToResponse).ToList());
    }
}
