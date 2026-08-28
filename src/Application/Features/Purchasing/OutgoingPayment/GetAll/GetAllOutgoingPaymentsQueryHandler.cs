namespace ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllOutgoingPaymentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllOutgoingPaymentsQuery, Result<List<OutgoingPaymentResponse>>>
{
    public async Task<Result<List<OutgoingPaymentResponse>>> HandleAsync(GetAllOutgoingPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OUTGOING_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<OutgoingPaymentResponse>>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to view outgoing payments."));

        var payments = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        return Result.Success(payments.Select(OutgoingPaymentMapper.ToResponse).ToList());
    }
}
