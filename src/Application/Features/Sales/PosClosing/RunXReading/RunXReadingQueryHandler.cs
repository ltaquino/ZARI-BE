namespace ZARI.Application.Features.Sales.PosClosing.RunXReading;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosClosing.Shared;
using ZARI.Domain.Common;

public sealed class RunXReadingQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<RunXReadingQuery, Result<XReadingResponse>>
{
    public async Task<Result<XReadingResponse>> HandleAsync(RunXReadingQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.View, query.BranchId, cancellationToken))
            return Result.Failure<XReadingResponse>(Error.Forbidden("PosClosing.Forbidden", "You do not have permission to view POS closing for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == query.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<XReadingResponse>(Error.NotFound("Branch.NotFound", $"Branch '{query.BranchId}' was not found."));

        var agg = await PosClosingAggregator.AggregateAsync(dbContext, query.BranchId, DateTimeOffset.UtcNow, cancellationToken);

        return Result.Success(new XReadingResponse(
            query.BranchId,
            agg.PeriodStart,
            agg.PeriodEnd,
            agg.InvoiceCount,
            agg.FirstOrNumber,
            agg.LastOrNumber,
            agg.GrossSales,
            agg.TotalDiscounts,
            agg.VatableSales,
            agg.VatAmount,
            agg.VatExemptSales,
            agg.ZeroRatedSales,
            agg.NetSales));
    }
}
