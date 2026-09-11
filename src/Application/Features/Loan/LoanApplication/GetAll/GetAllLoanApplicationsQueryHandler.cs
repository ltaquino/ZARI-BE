namespace ZARI.Application.Features.Loan.LoanApplications.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanApplicationsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllLoanApplicationsQuery, Result<List<LoanApplicationResponse>>>
{
    public async Task<Result<List<LoanApplicationResponse>>> HandleAsync(GetAllLoanApplicationsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_APPLICATIONS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanApplicationResponse>>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to view loan applications."));

        var applications = await dbContext.LoanApplications.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync(cancellationToken);

        return Result.Success(applications.Select(LoanApplicationMapper.ToResponse).ToList());
    }
}
