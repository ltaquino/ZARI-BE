namespace ZARI.Application.Features.Sales.SalesReturns.Submit;

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

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitSalesReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitSalesReturnCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(SubmitSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Edit, salesReturn.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to submit sales returns for this branch."));

        if (salesReturn.Status != "DRAFT")
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NotDraft", "Only draft sales returns can be submitted for approval."));

        if (salesReturn.Lines.Count == 0)
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(submitResult.Error!);

        salesReturn.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this sales return for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
