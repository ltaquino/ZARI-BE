namespace ZARI.Application.Features.Sales.CustomerPayments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllCustomerPaymentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllCustomerPaymentsQuery, Result<List<CustomerPaymentResponse>>>
{
    public async Task<Result<List<CustomerPaymentResponse>>> HandleAsync(GetAllCustomerPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CUSTOMER_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<CustomerPaymentResponse>>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to view customer payments."));

        var payments = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice)
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        return Result.Success(payments.Select(CustomerPaymentMapper.ToResponse).ToList());
    }
}
