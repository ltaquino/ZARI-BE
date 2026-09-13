namespace ZARI.Application.Features.Loan.LoanPayments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanPaymentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllLoanPaymentsQuery, Result<List<LoanPaymentResponse>>>
{
    public async Task<Result<List<LoanPaymentResponse>>> HandleAsync(GetAllLoanPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_PAYMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanPaymentResponse>>(Error.Forbidden("LoanPayment.Forbidden", "You do not have permission to view loan payments."));

        var payments = await dbContext.LoanPayments.AsNoTracking()
            .Include(p => p.LoanAccount).ThenInclude(a => a.Customer)
            .Include(p => p.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CostCenter)
            .Include(p => p.Allocations).ThenInclude(a => a.LoanAmortizationScheduleLine)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        return Result.Success(payments.Select(LoanPaymentMapper.ToResponse).ToList());
    }
}
