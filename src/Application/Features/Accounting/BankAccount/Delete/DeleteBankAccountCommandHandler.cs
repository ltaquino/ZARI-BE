namespace ZARI.Application.Features.Accounting.BankAccounts.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteBankAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteBankAccountCommand>
{
    public async Task<Result> HandleAsync(DeleteBankAccountCommand command, CancellationToken cancellationToken = default)
    {
        var bankAccount = await dbContext.BankAccounts.FindAsync([command.Id], cancellationToken);
        if (bankAccount is null)
            return Result.Failure(Error.NotFound("BankAccount.NotFound", $"Bank account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.Delete, bankAccount.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("BankAccount.Forbidden", "You do not have permission to delete bank accounts for this branch."));

        dbContext.BankAccounts.Remove(bankAccount);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
