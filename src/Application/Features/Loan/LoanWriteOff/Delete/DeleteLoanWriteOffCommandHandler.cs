namespace ZARI.Application.Features.Loan.LoanWriteOffs.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanWriteOffCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanWriteOffCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs.FindAsync([command.Id], cancellationToken);
        if (writeOff is null)
            return Result.Failure(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Delete, writeOff.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to delete this loan write-off for this branch."));

        if (writeOff.Status != "DRAFT")
            return Result.Failure(Error.Validation("LoanWriteOff.NotDraft", "Only a draft loan write-off can be deleted — cancel it instead."));

        dbContext.LoanWriteOffs.Remove(writeOff);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
