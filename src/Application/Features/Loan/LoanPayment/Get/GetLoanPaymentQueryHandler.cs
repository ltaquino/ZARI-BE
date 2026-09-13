namespace ZARI.Application.Features.Loan.LoanPayments.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanPaymentQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetLoanPaymentQuery, Result<LoanPaymentResponse>>
{
    public async Task<Result<LoanPaymentResponse>> HandleAsync(GetLoanPaymentQuery query, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.LoanPayments
            .Include(p => p.LoanAccount).ThenInclude(a => a.Customer)
            .Include(p => p.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CostCenter)
            .Include(p => p.Allocations).ThenInclude(a => a.LoanAmortizationScheduleLine)
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("LoanPayment.NotFound", $"Loan payment with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_PAYMENTS", FormAction.View, payment.BranchId, cancellationToken))
            return Result.Failure<LoanPaymentResponse>(Error.Forbidden("LoanPayment.Forbidden", "You do not have permission to view loan payments for this branch."));

        return Result.Success(LoanPaymentMapper.ToResponse(payment));
    }
}
