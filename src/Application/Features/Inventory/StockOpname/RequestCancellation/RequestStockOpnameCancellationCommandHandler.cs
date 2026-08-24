namespace ZARI.Application.Features.Inventory.StockOpnames.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestStockOpnameCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<RequestStockOpnameCancellationCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(RequestStockOpnameCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (opname.Status != "POSTED")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.NotPosted", "Only a posted stock count can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(submitResult.Error!);

        opname.Status = "PENDING_CANCELLATION";
        opname.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
