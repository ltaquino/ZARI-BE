namespace ZARI.Application.Features.Sales.SalesReturns.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestSalesReturnCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestSalesReturnCancellationCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(RequestSalesReturnCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Cancel, salesReturn.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to request cancellation of sales returns for this branch."));

        if (salesReturn.Status != "POSTED")
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NotPosted", "Only a posted sales return can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(submitResult.Error!);

        salesReturn.Status = "PENDING_CANCELLATION";
        salesReturn.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
