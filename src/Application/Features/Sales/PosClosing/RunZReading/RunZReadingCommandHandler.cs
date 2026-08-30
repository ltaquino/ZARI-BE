namespace ZARI.Application.Features.Sales.PosClosing.RunZReading;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosClosing.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Runs the end-of-day BIR close: same cutoff+aggregation logic X-Reading uses, but persists a
/// permanent ZReading row and increments Branch.ZCounter by exactly 1. No ApprovalRequest, no
/// retryable transaction (no stock engine involved) — a plain SaveChangesAsync at the end is
/// enough, same as ApproveSalesInvoiceCommandHandler. A zero-invoice period is a legitimate close
/// (FirstOrNumber/LastOrNumber stay null) — not blocked.
/// </summary>
public sealed class RunZReadingCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RunZReadingCommand, Result<ZReadingResponse>>
{
    public async Task<Result<ZReadingResponse>> HandleAsync(RunZReadingCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<ZReadingResponse>(Error.Forbidden("PosClosing.Forbidden", "You do not have permission to run a Z-Reading for this branch."));

        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == command.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<ZReadingResponse>(Error.NotFound("Branch.NotFound", $"Branch '{command.BranchId}' was not found."));

        var runAt = DateTimeOffset.UtcNow;
        var agg = await PosClosingAggregator.AggregateAsync(dbContext, command.BranchId, runAt, cancellationToken);

        branch.ZCounter += 1;

        var zReading = new ZReading
        {
            BranchId = command.BranchId,
            ZCounterValue = branch.ZCounter,
            FirstOrNumber = agg.FirstOrNumber,
            LastOrNumber = agg.LastOrNumber,
            PeriodStart = agg.PeriodStart,
            PeriodEnd = agg.PeriodEnd,
            InvoiceCount = agg.InvoiceCount,
            GrossSales = agg.GrossSales,
            TotalDiscounts = agg.TotalDiscounts,
            VatableSales = agg.VatableSales,
            VatAmount = agg.VatAmount,
            VatExemptSales = agg.VatExemptSales,
            ZeroRatedSales = agg.ZeroRatedSales,
            NetSales = agg.NetSales,
            CreatedBy = command.RunBy,
        };

        dbContext.ZReadings.Add(zReading);
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("POS_CLOSING", zReading.Id.ToString(), command.BranchId, "Z_READING_RUN", "ACTIVITY",
                "ran a Z-Reading closing", command.RunBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ZReadingResponse>(notifyResult.Error!);

        return Result.Success(new ZReadingResponse(
            zReading.Id,
            zReading.BranchId,
            zReading.ZCounterValue,
            zReading.PeriodStart,
            zReading.PeriodEnd,
            zReading.InvoiceCount,
            zReading.FirstOrNumber,
            zReading.LastOrNumber,
            zReading.GrossSales,
            zReading.TotalDiscounts,
            zReading.VatableSales,
            zReading.VatAmount,
            zReading.VatExemptSales,
            zReading.ZeroRatedSales,
            zReading.NetSales,
            zReading.CreatedAt,
            zReading.CreatedBy));
    }
}
