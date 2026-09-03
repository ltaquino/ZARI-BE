namespace ZARI.Application.Features.Sales.CustomerPayments.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllCustomerPaymentsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllCustomerPaymentsPagedQuery, Result<PagedResult<CustomerPaymentResponse>>>
{
    public async Task<Result<PagedResult<CustomerPaymentResponse>>> HandleAsync(GetAllCustomerPaymentsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CUSTOMER_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<CustomerPaymentResponse>>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to view customer payments."));

        var baseQuery = dbContext.CustomerPayments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.PaymentNo.Contains(query.Search) || (x.ReferenceNo != null && x.ReferenceNo.Contains(query.Search)));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var payments = await baseQuery
            .OrderByDescending(p => p.PaymentDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice)
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<CustomerPaymentResponse>(payments.Select(CustomerPaymentMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
