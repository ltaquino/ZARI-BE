namespace ZARI.Application.Features.Loan.LoanDisbursements.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanDisbursementsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllLoanDisbursementsQuery, Result<List<LoanDisbursementResponse>>>
{
    public async Task<Result<List<LoanDisbursementResponse>>> HandleAsync(GetAllLoanDisbursementsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_DISBURSEMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanDisbursementResponse>>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to view loan disbursements."));

        var disbursements = await dbContext.LoanDisbursements.AsNoTracking()
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .OrderByDescending(d => d.DisbursementDate)
            .ToListAsync(cancellationToken);

        return Result.Success(disbursements.Select(LoanDisbursementMapper.ToResponse).ToList());
    }
}
