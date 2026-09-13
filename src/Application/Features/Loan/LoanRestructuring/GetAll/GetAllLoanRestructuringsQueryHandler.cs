namespace ZARI.Application.Features.Loan.LoanRestructurings.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanRestructuringsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllLoanRestructuringsQuery, Result<List<LoanRestructuringResponse>>>
{
    public async Task<Result<List<LoanRestructuringResponse>>> HandleAsync(GetAllLoanRestructuringsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_RESTRUCTURINGS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanRestructuringResponse>>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to view loan restructurings."));

        var restructurings = await dbContext.LoanRestructurings.AsNoTracking()
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .OrderByDescending(r => r.RestructureDate)
            .ToListAsync(cancellationToken);

        return Result.Success(restructurings.Select(LoanRestructuringMapper.ToResponse).ToList());
    }
}
