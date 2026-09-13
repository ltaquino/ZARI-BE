namespace ZARI.Application.Features.Loan.LoanDisbursements.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanDisbursementCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanDisbursementCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements.FindAsync([command.Id], cancellationToken);
        if (disbursement is null)
            return Result.Failure(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Delete, disbursement.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to delete this loan disbursement for this branch."));

        if (disbursement.Status != "DRAFT")
            return Result.Failure(Error.Validation("LoanDisbursement.NotDraft", "Only a draft loan disbursement can be deleted — cancel it instead."));

        dbContext.LoanDisbursements.Remove(disbursement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
