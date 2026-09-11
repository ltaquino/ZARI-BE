namespace ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanWriteOffsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllLoanWriteOffsQuery, Result<List<LoanWriteOffResponse>>>
{
    public async Task<Result<List<LoanWriteOffResponse>>> HandleAsync(GetAllLoanWriteOffsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_WRITE_OFFS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanWriteOffResponse>>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to view loan write-offs."));

        var writeOffs = await dbContext.LoanWriteOffs.AsNoTracking()
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .OrderByDescending(w => w.WriteOffDate)
            .ToListAsync(cancellationToken);

        return Result.Success(writeOffs.Select(LoanWriteOffMapper.ToResponse).ToList());
    }
}
