namespace ZARI.Application.Features.Loan.LoanWriteOffs.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanWriteOffQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLoanWriteOffQuery, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(GetLoanWriteOffQuery query, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == query.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.View, writeOff.BranchId, cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to view loan write-offs for this branch."));

        return Result.Success(LoanWriteOffMapper.ToResponse(writeOff));
    }
}
