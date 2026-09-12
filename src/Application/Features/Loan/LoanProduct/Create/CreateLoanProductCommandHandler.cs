namespace ZARI.Application.Features.Loan.LoanProducts.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanProducts.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLoanProductCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateLoanProductCommand, Result<LoanProductResponse>>
{
    public async Task<Result<LoanProductResponse>> HandleAsync(CreateLoanProductCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_PRODUCTS", FormAction.Create, cancellationToken))
            return Result.Failure<LoanProductResponse>(Error.Forbidden("LoanProduct.Forbidden", "You do not have permission to create loan products."));

        var codeExists = await dbContext.LoanProducts.AnyAsync(p => p.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<LoanProductResponse>(Error.Conflict("LoanProduct.DuplicateCode", $"A loan product with code '{command.Code}' already exists."));

        foreach (var (accountId, label) in new[] { (command.LoanReceivableAccountId, "Loan Receivable"), (command.InterestIncomeAccountId, "Interest Income"), (command.PenaltyIncomeAccountId, "Penalty Income") })
        {
            if (accountId is null) continue;
            var accountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == accountId, cancellationToken);
            if (!accountExists)
                return Result.Failure<LoanProductResponse>(Error.NotFound("GlAccount.NotFound", $"{label} account with ID '{accountId}' was not found."));
        }

        var product = new LoanProduct
        {
            Code = command.Code,
            Name = command.Name,
            InterestMethod = command.InterestMethod,
            AnnualInterestRatePct = command.AnnualInterestRatePct,
            MinPrincipal = command.MinPrincipal,
            MaxPrincipal = command.MaxPrincipal,
            MinTermMonths = command.MinTermMonths,
            MaxTermMonths = command.MaxTermMonths,
            RepaymentFrequency = command.RepaymentFrequency,
            GracePeriodDays = command.GracePeriodDays,
            PenaltyRatePct = command.PenaltyRatePct,
            RequiresCollateral = command.RequiresCollateral,
            RequiresCoMaker = command.RequiresCoMaker,
            LoanReceivableAccountId = command.LoanReceivableAccountId,
            InterestIncomeAccountId = command.InterestIncomeAccountId,
            PenaltyIncomeAccountId = command.PenaltyIncomeAccountId,
            Status = command.Status,
            CicContractTypeCode = command.CicContractTypeCode
        };

        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new LoanProductResponse(product.Id, product.Code, product.Name, product.InterestMethod, product.AnnualInterestRatePct,
            product.MinPrincipal, product.MaxPrincipal, product.MinTermMonths, product.MaxTermMonths, product.RepaymentFrequency,
            product.GracePeriodDays, product.PenaltyRatePct, product.RequiresCollateral, product.RequiresCoMaker,
            product.LoanReceivableAccountId, product.InterestIncomeAccountId, product.PenaltyIncomeAccountId, product.Status,
            product.CicContractTypeCode, product.CreatedAt);

        return Result.Success(response);
    }
}
