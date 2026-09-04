namespace ZARI.Application.Features.Purchasing.Reports.CashOutRegister;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Ported from the FE's CashOutRegisterPage.tsx. Running balance only accumulates POSTED payments —
/// a cancelled or still-draft payment never actually moved cash, so it's still returned (for
/// visibility) but doesn't affect the running total. OutgoingPayment has no TotalAmount-equivalent
/// property, so the amount is always the sum of its lines.
/// </summary>
public sealed class GetCashOutRegisterReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetCashOutRegisterReportQuery, Result<CashOutRegisterReportResponse>>
{
    public async Task<Result<CashOutRegisterReportResponse>> HandleAsync(GetCashOutRegisterReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OUTGOING_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<CashOutRegisterReportResponse>(Error.Forbidden("CashOutRegisterReport.Forbidden", "You do not have permission to view outgoing payments."));

        var paymentsQuery = dbContext.OutgoingPayments.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.BranchId)) paymentsQuery = paymentsQuery.Where(p => p.BranchId == query.BranchId);
        if (query.BankAccountId is { } bankAccountId) paymentsQuery = paymentsQuery.Where(p => p.BankAccountId == bankAccountId);

        var payments = await paymentsQuery
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var running = 0m;
        var rows = new List<CashOutRegisterRow>();
        foreach (var payment in payments)
        {
            var amount = payment.Lines.Sum(l => l.Amount);
            if (payment.Status == "POSTED") running += amount;
            rows.Add(new CashOutRegisterRow(
                payment.Id,
                payment.PaymentDate,
                payment.PaymentNo,
                payment.Supplier.Name,
                payment.BankAccount.AccountName,
                payment.BranchId,
                payment.RefNo,
                payment.Status,
                amount,
                running));
        }

        return Result.Success(new CashOutRegisterReportResponse(rows, running));
    }
}
