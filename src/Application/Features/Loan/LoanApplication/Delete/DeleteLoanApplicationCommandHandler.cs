namespace ZARI.Application.Features.Loan.LoanApplications.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanApplicationCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanApplicationCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications.FindAsync([command.Id], cancellationToken);
        if (application is null)
            return Result.Failure(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Delete, application.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to delete this loan application for this branch."));

        if (application.Status != "DRAFT")
            return Result.Failure(Error.Validation("LoanApplication.NotDraft", "Only draft loan applications can be deleted — cancel it instead."));

        dbContext.LoanApplications.Remove(application);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
