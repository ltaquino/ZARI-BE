namespace ZARI.Application.Features.RouteSales.Trips.Shared;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Ordered quantity not delivered at a stop, with the reason (TripRefusalReason).</summary>
public sealed record TripRefusalLineInput(Guid SalesOrderLineId, decimal Qty, string ReasonCode, string? Notes = null);

internal static class TripRefusals
{
    /// <summary>Field rules for one refusal line (used by both the route sale and the stand-alone refusal).</summary>
    public static void LineRules(AbstractValidator<TripRefusalLineInput> v)
    {
        v.RuleFor(x => x.SalesOrderLineId).NotEmpty();
        v.RuleFor(x => x.Qty).GreaterThan(0);
        v.RuleFor(x => x.ReasonCode).Must(TripRefusalReason.IsValid)
            .WithMessage($"Reason must be one of: {string.Join(", ", TripRefusalReason.All.Select(r => r.Code))}.");
        v.RuleFor(x => x.Notes).NotEmpty().When(x => x.ReasonCode == TripRefusalReason.Other)
            .WithMessage("Explain the reason in the notes when it is OTHER.");
        v.RuleFor(x => x.Notes).MaximumLength(300);
    }

    /// <summary>
    /// Checks a stop's delivered + refused quantities against what is still open on this trip for each line of
    /// the order: open = ordered − posted deliveries − already refused on this trip. With
    /// <paramref name="requireEveryLine"/> (a sale against the order) every open quantity must be either
    /// delivered or refused with a reason — so a partial delivery always says why the rest wasn't received.
    /// </summary>
    public static async Task<Error?> CheckAsync(IAppDbContext dbContext, Guid tripId, SalesOrder order,
        Dictionary<Guid, decimal> deliveredNow, IReadOnlyCollection<TripRefusalLineInput> refusals, bool requireEveryLine, CancellationToken ct)
    {
        if (refusals.FirstOrDefault(r => order.Lines.All(l => l.Id != r.SalesOrderLineId)) is not null)
            return Error.Validation("TripRefusal.InvalidSalesOrderLine", $"A refused line isn't on sales order {order.SoNo}.");

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var delivered = await dbContext.DeliveryOrderLines
            .Where(l => l.SalesOrderLineId.HasValue && lineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
            .GroupBy(l => l.SalesOrderLineId!.Value).Select(g => new { g.Key, Qty = g.Sum(l => l.QtyShipped) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty, ct);
        var refusedBefore = await TripShared.RefusedOnTripAsync(dbContext, tripId, lineIds, ct);

        foreach (var line in order.Lines)
        {
            var code = line.Item?.Code ?? line.ItemId.ToString();
            var open = Math.Max(0, line.Qty - delivered.GetValueOrDefault(line.Id) - refusedBefore.GetValueOrDefault(line.Id));
            var dn = deliveredNow.GetValueOrDefault(line.Id);
            var rn = refusals.Where(r => r.SalesOrderLineId == line.Id).Sum(r => r.Qty);
            if (dn > open)
                return Error.Validation("RouteSale.ExceedsOrderedQty", $"{code}: only {open} still to deliver on {order.SoNo}.");
            if (dn + rn > open)
                return Error.Validation("TripRefusal.ExceedsOpen", $"{code}: delivered {dn} + not delivered {rn} is more than the {open} still open on {order.SoNo}.");
            if (requireEveryLine && open - dn - rn > 0)
                return Error.Validation("RouteSale.ShortfallReasonRequired",
                    $"{code}: {open - dn - rn} of {order.SoNo} not delivered — record why (refused or only partly received).");
        }
        return null;
    }

    public static TripRefusal Build(Guid tripId, Guid clientRef, Guid customerId, Guid salesOrderId, Guid? routeSaleId, DateTimeOffset at,
        IEnumerable<TripRefusalLineInput> lines, IReadOnlyDictionary<Guid, Guid> itemBySoLine, string? createdBy) => new()
    {
        TripId = tripId,
        ClientRef = clientRef,
        CustomerId = customerId,
        SalesOrderId = salesOrderId,
        RouteSaleId = routeSaleId,
        RefusedAt = at,
        CreatedBy = createdBy,
        Lines = lines.Select(l => new TripRefusalLine
        {
            SalesOrderLineId = l.SalesOrderLineId,
            ItemId = itemBySoLine[l.SalesOrderLineId],
            Qty = l.Qty,
            ReasonCode = l.ReasonCode,
            Notes = string.IsNullOrWhiteSpace(l.Notes) ? null : l.Notes.Trim(),
        }).ToList(),
    };
}
