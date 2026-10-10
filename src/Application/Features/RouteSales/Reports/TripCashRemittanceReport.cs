namespace ZARI.Application.Features.RouteSales.Reports.TripCashRemittance;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Trip Cash Remittance (RouteSalesContext.md §6.3): what the driver took in, by tender, split between
/// sales at the stops and collections of older invoices; what was left on account; the invoice numbers
/// voided; and the cash the driver should hand in vs what they did. Tender amounts are the posted
/// payments' own (net of change given).
/// </summary>
public sealed record GetTripCashRemittanceReportQuery(Guid TripId) : IQuery<Result<TripCashRemittanceReportResponse>>;

public sealed record TripTenderSummary(string MethodCode, string MethodName, decimal FromSales, decimal FromCollections, decimal Total);

public sealed record TripCollectionRow(
    Guid TripCollectionId,
    DateTimeOffset CollectedAt,
    string CustomerName,
    string? PaymentNo,
    decimal Amount,
    decimal CashAmount,
    string Status,
    /// <summary>The invoices it paid, by BIR number (internal number when there is none).</summary>
    List<string> InvoicesPaid);

public sealed record TripVoidRow(string BirOrSeriesNumber, DateTimeOffset VoidedAt, string? Reason);

public sealed record TripShortagePaymentRow(DateTimeOffset PaidAt, string PaymentMethodName, decimal Amount, string? ReferenceNo);

public sealed record TripCashRemittanceReportResponse(
    TripReportHeader Header,
    List<TripTenderSummary> Tenders,
    List<TripCollectionRow> Collections,
    List<TripVoidRow> Voids,
    decimal SalesInvoiced,
    decimal SalesPaid,
    decimal SalesCharged,
    decimal CollectionsTotal,
    decimal TotalReceived,
    decimal CashExpected,
    decimal? CashRemitted,
    /// <summary>Remitted − expected; negative = short. Null until the driver ends the trip.</summary>
    decimal? CashShortOver,
    /// <summary>PAYROLL, DRIVER_PAYS or WRITE_OFF — how the close settled the shortage; null when there was none.</summary>
    string? ShortageSettlement,
    /// <summary>The shortage found at close (stock at cost + short cash), whoever bears it.</summary>
    decimal ShortageAmount,
    /// <summary>Stock + cash shortage charged to the driver at close (zero when written off).</summary>
    decimal ShortageCharged,
    decimal ShortagePaid,
    decimal ShortageBalance,
    List<TripShortagePaymentRow> ShortagePayments);

public sealed class GetTripCashRemittanceReportQueryHandler(
    IAppDbContext dbContext, IPermissionService permissionService, ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetTripCashRemittanceReportQuery, Result<TripCashRemittanceReportResponse>>
{
    public async Task<Result<TripCashRemittanceReportResponse>> HandleAsync(GetTripCashRemittanceReportQuery query, CancellationToken cancellationToken = default)
    {
        var loaded = await TripReportShared.LoadTripAsync(dbContext, permissionService, currentUser, query.TripId, cancellationToken);
        if (!loaded.IsSuccess) return Result.Failure<TripCashRemittanceReportResponse>(loaded.Error!);
        var trip = loaded.Value!;
        var ct = cancellationToken;

        var sales = await dbContext.RouteSales.AsNoTracking().Where(r => r.TripId == trip.Id).ToListAsync(ct);
        var collections = await dbContext.TripCollections.AsNoTracking().Include(c => c.Customer)
            .Where(c => c.TripId == trip.Id).OrderBy(c => c.CollectedAt).ToListAsync(ct);

        var salePaymentIds = sales.Where(s => s.Status == "COMPLETED" && s.CustomerPaymentId.HasValue).Select(s => s.CustomerPaymentId!.Value).ToList();
        var collectionPaymentIds = collections.Where(c => c.Status == "COMPLETED" && c.CustomerPaymentId.HasValue).Select(c => c.CustomerPaymentId!.Value).ToList();
        var paymentIds = salePaymentIds.Concat(collectionPaymentIds).ToList();
        var payments = await dbContext.CustomerPayments.AsNoTracking()
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice)
            .Where(p => paymentIds.Contains(p.Id) && p.Status != "CANCELLED")
            .ToListAsync(ct);

        // One row per tender; a payment without tenders (legacy single-method) counts under its header method.
        var tenderRows = payments.SelectMany(p =>
        {
            var fromSale = salePaymentIds.Contains(p.Id);
            return p.Tenders.Count > 0
                ? p.Tenders.Select(t => (t.PaymentMethod.Code, t.PaymentMethod.Name, t.PaymentMethod.DisplayOrder, fromSale, t.Amount))
                : [(p.PaymentMethod, p.PaymentMethod, int.MaxValue, fromSale, p.Lines.Sum(l => l.AmountApplied))];
        }).ToList();
        var tenders = tenderRows.GroupBy(t => t.Code)
            .OrderBy(g => g.Min(t => t.DisplayOrder)).ThenBy(g => g.Key)
            .Select(g =>
            {
                var sale = g.Where(t => t.fromSale).Sum(t => t.Amount);
                var collected = g.Where(t => !t.fromSale).Sum(t => t.Amount);
                return new TripTenderSummary(g.Key, g.First().Name, sale, collected, sale + collected);
            }).ToList();

        var paymentsById = payments.ToDictionary(p => p.Id);
        var collectionRows = collections.Select(c =>
        {
            var payment = c.CustomerPaymentId is { } id ? paymentsById.GetValueOrDefault(id) : null;
            var invoices = payment?.Lines.Select(l => l.SalesInvoice.BirOrSeriesNumber ?? l.SalesInvoice.InvoiceNo).ToList() ?? [];
            return new TripCollectionRow(c.Id, c.CollectedAt, c.Customer.Name, payment?.PaymentNo, c.Amount, c.CashAmount, c.Status, invoices);
        }).ToList();

        var voids = sales.Where(s => s.Status == "VOID").OrderBy(s => s.BirOrSeriesNumber, StringComparer.Ordinal)
            .Select(s => new TripVoidRow(s.BirOrSeriesNumber, s.CompletedAt ?? s.SoldAt, s.VoidReason)).ToList();

        var shortagePayments = await dbContext.TripShortagePayments.AsNoTracking().Include(p => p.PaymentMethod)
            .Where(p => p.TripId == trip.Id).OrderBy(p => p.PaidAt)
            .Select(p => new TripShortagePaymentRow(p.PaidAt, p.PaymentMethod.Name, p.Amount, p.ReferenceNo)).ToListAsync(ct);

        var completed = sales.Where(s => s.Status == "COMPLETED").ToList();
        var salesInvoiced = completed.Sum(s => s.InvoiceTotal);
        var salesPaid = completed.Sum(s => s.AmountPaid);
        var collectionsTotal = collections.Where(c => c.Status == "COMPLETED").Sum(c => c.Amount);

        return Result.Success(new TripCashRemittanceReportResponse(
            await TripReportShared.HeaderAsync(userManager, trip, ct), tenders, collectionRows, voids,
            salesInvoiced, salesPaid, Math.Max(0, salesInvoiced - salesPaid), collectionsTotal, tenders.Sum(t => t.Total),
            trip.CashExpected, trip.CashRemitted, TripReportShared.CashShortOver(trip),
            trip.ShortageSettlement, Math.Max(trip.ShortageAmount, trip.ShortageCharged), trip.ShortageCharged, trip.ShortagePaid, TripShared.ShortageBalance(trip), shortagePayments));
    }
}
