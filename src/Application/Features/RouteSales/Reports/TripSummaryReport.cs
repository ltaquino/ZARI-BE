namespace ZARI.Application.Features.RouteSales.Reports.TripSummary;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Trip Summary (RouteSalesContext.md §6.1): per item, what was ordered, loaded, delivered, sold off the
/// truck, returned, brought back and lost — every quantity is a posted document's own total (TripLine is
/// maintained by the posting handlers). Values are at the cost the stock engine charged for the trip.
/// </summary>
public sealed record GetTripSummaryReportQuery(Guid TripId) : IQuery<Result<TripSummaryReportResponse>>;

public sealed record TripSummaryLine(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    decimal QtyOrdered,
    decimal QtyExtra,
    decimal QtyLoaded,
    decimal? QtyLoadCheck,
    decimal QtyDelivered,
    decimal QtyVanSold,
    decimal QtyRefused,
    decimal QtyReturnedGood,
    decimal QtyReturnedDamaged,
    decimal? QtyCountedAtEnd,
    decimal QtyLoadedBack,
    decimal QtyDamagedOut,
    decimal QtyVariance,
    decimal QtyExpectedOnTruck,
    decimal UnitCost,
    decimal LoadedValue,
    decimal SoldValue,
    decimal VarianceValue);

public sealed record TripSummaryTotals(
    decimal QtyOrdered,
    decimal QtyExtra,
    decimal QtyLoaded,
    decimal QtyDelivered,
    decimal QtyVanSold,
    decimal QtyRefused,
    decimal QtyReturnedGood,
    decimal QtyReturnedDamaged,
    decimal QtyLoadedBack,
    decimal QtyDamagedOut,
    decimal QtyVariance,
    decimal LoadedValue,
    decimal SoldValue,
    decimal VarianceValue,
    /// <summary>Invoice totals of the trip's completed route sales (selling price, VAT-inclusive).</summary>
    decimal SalesAmount);

public sealed record TripSummaryReportResponse(TripReportHeader Header, List<TripSummaryLine> Lines, TripSummaryTotals Totals);

public sealed class GetTripSummaryReportQueryHandler(
    IAppDbContext dbContext, IPermissionService permissionService, ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetTripSummaryReportQuery, Result<TripSummaryReportResponse>>
{
    public async Task<Result<TripSummaryReportResponse>> HandleAsync(GetTripSummaryReportQuery query, CancellationToken cancellationToken = default)
    {
        var loaded = await TripReportShared.LoadTripAsync(dbContext, permissionService, currentUser, query.TripId, cancellationToken);
        if (!loaded.IsSuccess) return Result.Failure<TripSummaryReportResponse>(loaded.Error!);
        var trip = loaded.Value!;

        var moves = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines)
            .Where(m => m.TripId == trip.Id).ToListAsync(cancellationToken);
        var costs = TripReportShared.UnitCosts(moves);
        var closed = trip.Status == TripStatus.Closed;

        var lines = trip.Lines.OrderBy(l => l.Item.Code).Select(l =>
        {
            var cost = costs.GetValueOrDefault(l.ItemId);
            // Once closed, the variance is what the close actually posted; before that, an estimate at trip cost.
            var varianceValue = closed ? TripReportShared.PostedVarianceValue(moves, l.ItemId) : l.QtyVariance * cost;
            return new TripSummaryLine(
                l.ItemId, l.Item.Code, l.Item.Name, l.QtyOrdered, l.QtyExtra, l.QtyLoaded, l.QtyLoadCheck, l.QtyDelivered, l.QtyVanSold,
                l.QtyRefused, l.QtyReturnedGood, l.QtyReturnedDamaged, l.QtyCountedAtEnd, l.QtyLoadedBack, l.QtyDamagedOut, l.QtyVariance,
                TripShared.ExpectedGoodOnTruck(l), cost,
                Math.Round(l.QtyLoaded * cost, 2), Math.Round((l.QtyDelivered + l.QtyVanSold) * cost, 2), Math.Round(varianceValue, 2));
        }).ToList();

        var salesAmount = await dbContext.RouteSales.AsNoTracking()
            .Where(r => r.TripId == trip.Id && r.Status == "COMPLETED")
            .SumAsync(r => r.InvoiceTotal, cancellationToken);

        var totals = new TripSummaryTotals(
            lines.Sum(l => l.QtyOrdered), lines.Sum(l => l.QtyExtra), lines.Sum(l => l.QtyLoaded), lines.Sum(l => l.QtyDelivered),
            lines.Sum(l => l.QtyVanSold), lines.Sum(l => l.QtyRefused), lines.Sum(l => l.QtyReturnedGood), lines.Sum(l => l.QtyReturnedDamaged),
            lines.Sum(l => l.QtyLoadedBack), lines.Sum(l => l.QtyDamagedOut), lines.Sum(l => l.QtyVariance),
            lines.Sum(l => l.LoadedValue), lines.Sum(l => l.SoldValue), lines.Sum(l => l.VarianceValue), Math.Round(salesAmount, 2));

        return Result.Success(new TripSummaryReportResponse(await TripReportShared.HeaderAsync(userManager, trip, cancellationToken), lines, totals));
    }
}
