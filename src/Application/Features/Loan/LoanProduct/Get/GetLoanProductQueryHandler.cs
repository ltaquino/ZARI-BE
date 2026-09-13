namespace ZARI.Application.Features.Loan.LoanProducts.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetLoanProductQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetLoanProductQuery, Result<LoanProductResponse>>
{
    public async Task<Result<LoanProductResponse>> HandleAsync(GetLoanProductQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_PRODUCTS", FormAction.View, cancellationToken))
            return Result.Failure<LoanProductResponse>(Error.Forbidden("LoanProduct.Forbidden", "You do not have permission to view loan products."));

        var product = await dbContext.LoanProducts
            .Where(p => p.Id == query.Id)
            .Select(p => new LoanProductResponse(p.Id, p.Code, p.Name, p.InterestMethod, p.AnnualInterestRatePct, p.MinPrincipal, p.MaxPrincipal,
                p.MinTermMonths, p.MaxTermMonths, p.RepaymentFrequency, p.GracePeriodDays, p.PenaltyRatePct, p.RequiresCollateral, p.RequiresCoMaker,
                p.LoanReceivableAccountId, p.InterestIncomeAccountId, p.PenaltyIncomeAccountId, p.Status, p.CicContractTypeCode, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Result.Failure<LoanProductResponse>(Error.NotFound("LoanProduct.NotFound", $"Loan product with ID '{query.Id}' was not found."));

        return Result.Success(product);
    }
}
