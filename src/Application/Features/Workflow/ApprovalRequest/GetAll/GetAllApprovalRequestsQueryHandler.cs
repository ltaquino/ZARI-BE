namespace ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllApprovalRequestsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllApprovalRequestsQuery, Result<List<ApprovalRequestResponse>>>
{
    public async Task<Result<List<ApprovalRequestResponse>>> HandleAsync(GetAllApprovalRequestsQuery query, CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.ApprovalRequests
            .Include(r => r.Actions)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

        return Result.Success(requests.Select(ApprovalRequestMapper.ToResponse).ToList());
    }
}
