namespace ZARI.Application.Features.Sales.PosClosing.GetAllZReadings;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosClosing.RunZReading;
using ZARI.Application.Features.Sales.PosClosing.Shared;
using ZARI.Domain.Common;

public sealed class GetAllZReadingsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllZReadingsQuery, Result<List<ZReadingResponse>>>
{
    public async Task<Result<List<ZReadingResponse>>> HandleAsync(GetAllZReadingsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.View, query.BranchId, cancellationToken))
            return Result.Failure<List<ZReadingResponse>>(Error.Forbidden("PosClosing.Forbidden", "You do not have permission to view POS closing for this branch."));

        var scope = PosClosingAggregator.ScopeOf(query.PosTerminalId);
        var readings = await dbContext.ZReadings.AsNoTracking()
            .Where(z => z.BranchId == query.BranchId && z.CounterScope == scope)
            .OrderByDescending(z => z.ZCounterValue)
            .ToListAsync(cancellationToken);

        return Result.Success(readings.Select(z => new ZReadingResponse(
            z.Id,
            z.BranchId,
            z.ZCounterValue,
            z.PeriodStart,
            z.PeriodEnd,
            z.InvoiceCount,
            z.FirstOrNumber,
            z.LastOrNumber,
            z.GrossSales,
            z.TotalDiscounts,
            z.VatableSales,
            z.VatAmount,
            z.VatExemptSales,
            z.ZeroRatedSales,
            z.NetSales,
            z.CreatedAt,
            z.CreatedBy,
            z.PosTerminalId)).ToList());
    }
}
