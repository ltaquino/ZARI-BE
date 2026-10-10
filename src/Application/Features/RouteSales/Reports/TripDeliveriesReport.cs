namespace ZARI.Application.Features.RouteSales.Reports.TripDeliveries;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Trip Deliveries (RouteSalesContext.md §6.2): every invoice number the truck used, in the order it
/// was issued — completed sales with their delivery/invoice/payment and proof of delivery, voids with
/// their reason — plus each assigned sales-order line's ordered vs delivered-on-this-trip vs still open, and
/// every quantity not delivered with the driver's reason (refused, or only partly received).
/// </summary>
public sealed record GetTripDeliveriesReportQuery(Guid TripId) : IQuery<Result<TripDeliveriesReportResponse>>;

public sealed record TripDeliveryStop(
    Guid RouteSaleId,
    string BirOrSeriesNumber,
    DateTimeOffset SoldAt,
    /// <summary>COMPLETED, VOID, or PENDING (sent but not finished — the handheld retries it).</summary>
    string Status,
    string? CustomerName,
    string? SoNo,
    string? DoNo,
    string? InvoiceNo,
    string? PaymentNo,
    decimal InvoiceTotal,
    decimal AmountPaid,
    /// <summary>Left on the customer's account (charge sale).</summary>
    decimal AmountCharged,
    decimal CashCollected,
    string? ReceivedByName,
    string? SignatureUrl,
    string? PhotoUrl,
    decimal? Latitude,
    decimal? Longitude,
    string? VoidReason);

public sealed record TripOrderLineStatus(
    Guid SalesOrderId,
    string SoNo,
    string CustomerName,
    Guid SalesOrderLineId,
    string ItemCode,
    string ItemName,
    decimal QtyOrdered,
    decimal QtyDeliveredThisTrip,
    decimal QtyDeliveredOtherwise,
    /// <summary>Not delivered on this trip, with a reason (see Refusals).</summary>
    decimal QtyRefusedThisTrip,
    /// <summary>Still to deliver — carried to a later trip (see the Undelivered report).</summary>
    decimal QtyOpen,
    /// <summary>This trip's reasons for the line, e.g. "Customer can't pay (2)".</summary>
    string? RefusalReasons);

/// <summary>One quantity not delivered at a stop and why. BirOrSeriesNumber is set when part of the order was
/// delivered on that invoice (partly received); null when the customer accepted nothing.</summary>
public sealed record TripRefusalRow(
    DateTimeOffset RefusedAt,
    string CustomerName,
    string SoNo,
    string? BirOrSeriesNumber,
    string ItemCode,
    string ItemName,
    decimal Qty,
    string ReasonCode,
    string Reason,
    string? Notes);

public sealed record TripDeliveriesTotals(
    int Invoices,
    int Voids,
    int Pending,
    decimal InvoiceTotal,
    decimal AmountPaid,
    decimal AmountCharged,
    decimal CashCollected);

public sealed record TripDeliveriesReportResponse(
    TripReportHeader Header,
    List<TripDeliveryStop> Stops,
    List<TripOrderLineStatus> OrderLines,
    List<TripRefusalRow> Refusals,
    TripDeliveriesTotals Totals);

public sealed class GetTripDeliveriesReportQueryHandler(
    IAppDbContext dbContext, IPermissionService permissionService, ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetTripDeliveriesReportQuery, Result<TripDeliveriesReportResponse>>
{
    public async Task<Result<TripDeliveriesReportResponse>> HandleAsync(GetTripDeliveriesReportQuery query, CancellationToken cancellationToken = default)
    {
        var loaded = await TripReportShared.LoadTripAsync(dbContext, permissionService, currentUser, query.TripId, cancellationToken);
        if (!loaded.IsSuccess) return Result.Failure<TripDeliveriesReportResponse>(loaded.Error!);
        var trip = loaded.Value!;
        var ct = cancellationToken;

        var sales = await dbContext.RouteSales.AsNoTracking().Include(r => r.Customer)
            .Where(r => r.TripId == trip.Id).ToListAsync(ct);
        // Issue order is the series order; the zero-padded number sorts the same as numerically.
        sales = sales.OrderBy(r => r.BirOrSeriesNumber, StringComparer.Ordinal).ToList();

        var soIds = trip.SalesOrders.Select(s => s.SalesOrderId).Concat(sales.Where(s => s.SalesOrderId.HasValue).Select(s => s.SalesOrderId!.Value))
            .Distinct().ToList();
        var deliveryIds = sales.Where(s => s.DeliveryOrderId.HasValue).Select(s => s.DeliveryOrderId!.Value).ToList();
        var invoiceIds = sales.Where(s => s.SalesInvoiceId.HasValue).Select(s => s.SalesInvoiceId!.Value).ToList();
        var paymentIds = sales.Where(s => s.CustomerPaymentId.HasValue).Select(s => s.CustomerPaymentId!.Value).ToList();

        var soNos = await dbContext.SalesOrders.AsNoTracking().Where(o => soIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.SoNo, ct);
        var doNos = await dbContext.DeliveryOrders.AsNoTracking().Where(d => deliveryIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DoNo, ct);
        var invoiceNos = await dbContext.SalesInvoices.AsNoTracking().Where(i => invoiceIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.InvoiceNo, ct);
        var paymentNos = await dbContext.CustomerPayments.AsNoTracking().Where(p => paymentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PaymentNo, ct);

        var stops = sales.Select(s => new TripDeliveryStop(
            s.Id, s.BirOrSeriesNumber, s.SoldAt, s.Status, s.Customer?.Name,
            s.SalesOrderId is { } so ? soNos.GetValueOrDefault(so) : null,
            s.DeliveryOrderId is { } d ? doNos.GetValueOrDefault(d) : null,
            s.SalesInvoiceId is { } i ? invoiceNos.GetValueOrDefault(i) : null,
            s.CustomerPaymentId is { } p ? paymentNos.GetValueOrDefault(p) : null,
            s.InvoiceTotal, s.AmountPaid, s.Status == "COMPLETED" ? Math.Max(0, s.InvoiceTotal - s.AmountPaid) : 0, s.CashCollected,
            s.ReceivedByName, s.SignatureFilePath, s.PhotoFilePath, s.Latitude, s.Longitude, s.VoidReason)).ToList();

        var tripSoIds = trip.SalesOrders.Select(s => s.SalesOrderId).ToList();
        var orders = await dbContext.SalesOrders.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.Lines).ThenInclude(l => l.Item)
            .Where(o => tripSoIds.Contains(o.Id)).ToListAsync(ct);
        var soLineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
        var deliveredLines = await dbContext.DeliveryOrderLines.AsNoTracking()
            .Where(l => l.SalesOrderLineId.HasValue && soLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
            .Select(l => new { SoLineId = l.SalesOrderLineId!.Value, l.DeliveryOrderId, l.QtyShipped })
            .ToListAsync(ct);

        var refusals = await dbContext.TripRefusals.AsNoTracking()
            .Include(r => r.Customer).Include(r => r.SalesOrder).Include(r => r.Lines).ThenInclude(l => l.Item)
            .Where(r => r.TripId == trip.Id).ToListAsync(ct);
        var birBySale = sales.ToDictionary(s => s.Id, s => s.BirOrSeriesNumber);
        var refusalRows = refusals.OrderBy(r => r.RefusedAt).SelectMany(r => r.Lines.OrderBy(l => l.Item.Code).Select(l => new TripRefusalRow(
            r.RefusedAt, r.Customer.Name, r.SalesOrder.SoNo, r.RouteSaleId is { } rs ? birBySale.GetValueOrDefault(rs) : null,
            l.Item.Code, l.Item.Name, l.Qty, l.ReasonCode, TripRefusalReason.Label(l.ReasonCode), l.Notes))).ToList();
        var refusalLines = refusals.SelectMany(r => r.Lines).ToList();

        var orderLines = orders.OrderBy(o => o.SoNo).SelectMany(o => o.Lines.OrderBy(l => l.Item.Code).Select(l =>
        {
            var thisTrip = deliveredLines.Where(d => d.SoLineId == l.Id && deliveryIds.Contains(d.DeliveryOrderId)).Sum(d => d.QtyShipped);
            var all = deliveredLines.Where(d => d.SoLineId == l.Id).Sum(d => d.QtyShipped);
            var refused = refusalLines.Where(r => r.SalesOrderLineId == l.Id).ToList();
            var reasons = refused.Count == 0 ? null : string.Join("; ", refused.GroupBy(r => r.ReasonCode)
                .Select(g => $"{TripRefusalReason.Label(g.Key)} ({g.Sum(r => r.Qty):#,0.####})"));
            return new TripOrderLineStatus(o.Id, o.SoNo, o.Customer.Name, l.Id, l.Item.Code, l.Item.Name, l.Qty, thisTrip, all - thisTrip,
                refused.Sum(r => r.Qty), Math.Max(0, l.Qty - all), reasons);
        })).ToList();

        var completed = stops.Where(s => s.Status == "COMPLETED").ToList();
        var totals = new TripDeliveriesTotals(
            completed.Count, stops.Count(s => s.Status == "VOID"), stops.Count(s => s.Status == "PENDING"),
            completed.Sum(s => s.InvoiceTotal), completed.Sum(s => s.AmountPaid), completed.Sum(s => s.AmountCharged), completed.Sum(s => s.CashCollected));

        return Result.Success(new TripDeliveriesReportResponse(await TripReportShared.HeaderAsync(userManager, trip, ct), stops, orderLines, refusalRows, totals));
    }
}
