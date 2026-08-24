namespace ZARI.Application.Features.Workflow.ApprovalRequests.Submit;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class SubmitForApprovalCommandHandler(IAppDbContext dbContext) : ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>
{
    public async Task<Result<ApprovalRequestResponse>> HandleAsync(SubmitForApprovalCommand command, CancellationToken cancellationToken = default)
    {
        var request = new ApprovalRequest
        {
            EntityType = command.EntityType,
            EntityId = command.EntityId,
            BranchId = command.BranchId,
            RequestedBy = command.RequestedBy,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = "PENDING",
            RequestType = command.RequestType ?? "SUBMIT",
            Reason = command.Reason
        };

        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ApprovalRequestMapper.ToResponse(request));
    }
}
