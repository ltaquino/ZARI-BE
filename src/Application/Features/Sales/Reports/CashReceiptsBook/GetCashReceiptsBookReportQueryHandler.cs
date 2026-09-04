namespace ZARI.Application.Features.Sales.Reports.CashReceiptsBook;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// BIR Cash Receipts Book — mirrors the Purchasing module's Cash-Out Register exactly, just on
/// CustomerPayment/CustomerPaymentLines instead of OutgoingPayment. Amount is always the sum of the
/// payment's own lines' AmountApplied (CustomerPayment carries no separate stored total, unlike
/// OutgoingPayment — see CustomerPaymentMapper.ToResponse, which computes its own TotalAmount the
/// same way). The running total accumulates POSTED payments only; every row is still returned.
/// </summary>
public sealed class GetCashReceiptsBookReportQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetCashReceiptsBookReportQuery, Result<CashReceiptsBookReportResponse>>
{
    public async Task<Result<CashReceiptsBookReportResponse>> HandleAsync(GetCashReceiptsBookReportQuery query, CancellationToken cancellationToken = default)
    {
        var payments = await dbContext.CustomerPayments.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Lines)
            .Where(p => query.BranchId == null || p.BranchId == query.BranchId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var rows = new List<CashReceiptsBookRow>(payments.Count);
        decimal runningTotal = 0;

        foreach (var payment in payments)
        {
            var amount = payment.Lines.Sum(l => l.AmountApplied);
            if (payment.Status == "POSTED") runningTotal += amount;

            rows.Add(new CashReceiptsBookRow(
                payment.Id,
                payment.PaymentDate,
                payment.PaymentNo,
                payment.Customer.Name,
                payment.PaymentMethod,
                payment.BranchId,
                payment.ReferenceNo,
                payment.Status,
                Math.Round(amount, 2),
                Math.Round(runningTotal, 2)));
        }

        return Result.Success(new CashReceiptsBookReportResponse(rows, Math.Round(runningTotal, 2)));
    }
}
