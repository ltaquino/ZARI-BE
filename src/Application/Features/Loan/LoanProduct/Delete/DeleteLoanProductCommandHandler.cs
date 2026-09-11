namespace ZARI.Application.Features.Loan.LoanProducts.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanProductCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanProductCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.LoanProducts.FindAsync([command.Id], cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound("LoanProduct.NotFound", $"Loan product with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("LOAN_PRODUCTS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanProduct.Forbidden", "You do not have permission to delete loan products."));

        dbContext.LoanProducts.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
