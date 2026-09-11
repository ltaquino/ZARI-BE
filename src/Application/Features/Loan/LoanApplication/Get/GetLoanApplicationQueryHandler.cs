namespace ZARI.Application.Features.Loan.LoanApplications.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanApplicationQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetLoanApplicationQuery, Result<LoanApplicationResponse>>
{
    public async Task<Result<LoanApplicationResponse>> HandleAsync(GetLoanApplicationQuery query, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

        if (application is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.View, application.BranchId, cancellationToken))
            return Result.Failure<LoanApplicationResponse>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to view loan applications for this branch."));

        return Result.Success(LoanApplicationMapper.ToResponse(application));
    }
}
