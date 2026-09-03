namespace ZARI.Application.Features.Purchasing.OutgoingPayments.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllOutgoingPaymentsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllOutgoingPaymentsPagedQuery, Result<PagedResult<OutgoingPaymentResponse>>>
{
    public async Task<Result<PagedResult<OutgoingPaymentResponse>>> HandleAsync(GetAllOutgoingPaymentsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OUTGOING_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<OutgoingPaymentResponse>>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to view outgoing payments."));

        var baseQuery = dbContext.OutgoingPayments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.PaymentNo.Contains(query.Search) || (x.RefNo != null && x.RefNo.Contains(query.Search)));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var payments = await baseQuery
            .OrderByDescending(p => p.PaymentDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<OutgoingPaymentResponse>(payments.Select(OutgoingPaymentMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
