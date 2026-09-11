namespace ZARI.Application.Features.Loan.LoanProducts.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateLoanProductCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateLoanProductCommand>
{
    public async Task<Result> HandleAsync(UpdateLoanProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.LoanProducts.FindAsync([command.Id], cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound("LoanProduct.NotFound", $"Loan product with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("LOAN_PRODUCTS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanProduct.Forbidden", "You do not have permission to update loan products."));

        var duplicateCode = await dbContext.LoanProducts.AnyAsync(p => p.Id != command.Id && p.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("LoanProduct.DuplicateCode", $"A loan product with code '{command.Code}' already exists."));

        foreach (var (accountId, label) in new[] { (command.LoanReceivableAccountId, "Loan Receivable"), (command.InterestIncomeAccountId, "Interest Income"), (command.PenaltyIncomeAccountId, "Penalty Income") })
        {
            if (accountId is null) continue;
            var accountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == accountId, cancellationToken);
            if (!accountExists)
                return Result.Failure(Error.NotFound("GlAccount.NotFound", $"{label} account with ID '{accountId}' was not found."));
        }

        product.Code = command.Code;
        product.Name = command.Name;
        product.InterestMethod = command.InterestMethod;
        product.AnnualInterestRatePct = command.AnnualInterestRatePct;
        product.MinPrincipal = command.MinPrincipal;
        product.MaxPrincipal = command.MaxPrincipal;
        product.MinTermMonths = command.MinTermMonths;
        product.MaxTermMonths = command.MaxTermMonths;
        product.RepaymentFrequency = command.RepaymentFrequency;
        product.GracePeriodDays = command.GracePeriodDays;
        product.PenaltyRatePct = command.PenaltyRatePct;
        product.RequiresCollateral = command.RequiresCollateral;
        product.RequiresCoMaker = command.RequiresCoMaker;
        product.LoanReceivableAccountId = command.LoanReceivableAccountId;
        product.InterestIncomeAccountId = command.InterestIncomeAccountId;
        product.PenaltyIncomeAccountId = command.PenaltyIncomeAccountId;
        product.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
