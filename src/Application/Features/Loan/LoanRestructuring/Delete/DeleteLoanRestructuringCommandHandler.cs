namespace ZARI.Application.Features.Loan.LoanRestructurings.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanRestructuringCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanRestructuringCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings.FindAsync([command.Id], cancellationToken);
        if (restructuring is null)
            return Result.Failure(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Delete, restructuring.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to delete this loan restructuring for this branch."));

        if (restructuring.Status != "DRAFT")
            return Result.Failure(Error.Validation("LoanRestructuring.NotDraft", "Only a draft loan restructuring can be deleted — cancel it instead."));

        dbContext.LoanRestructurings.Remove(restructuring);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
