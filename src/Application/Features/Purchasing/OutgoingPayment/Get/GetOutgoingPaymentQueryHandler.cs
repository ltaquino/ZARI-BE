namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetOutgoingPaymentQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetOutgoingPaymentQuery, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(GetOutgoingPaymentQuery query, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.View, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to view outgoing payments for this branch."));

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
