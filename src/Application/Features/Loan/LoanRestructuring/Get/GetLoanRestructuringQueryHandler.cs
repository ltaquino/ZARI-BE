namespace ZARI.Application.Features.Loan.LoanRestructurings.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanRestructuringQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLoanRestructuringQuery, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(GetLoanRestructuringQuery query, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.View, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to view loan restructurings for this branch."));

        return Result.Success(LoanRestructuringMapper.ToResponse(restructuring));
    }
}
