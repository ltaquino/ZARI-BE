namespace ZARI.Application.Features.Loan.LoanProducts.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanProducts.Get;
using ZARI.Domain.Common;

public sealed class GetAllLoanProductsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllLoanProductsQuery, Result<List<LoanProductResponse>>>
{
    public async Task<Result<List<LoanProductResponse>>> HandleAsync(GetAllLoanProductsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_PRODUCTS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanProductResponse>>(Error.Forbidden("LoanProduct.Forbidden", "You do not have permission to view loan products."));

        var products = await dbContext.LoanProducts.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new LoanProductResponse(p.Id, p.Code, p.Name, p.InterestMethod, p.AnnualInterestRatePct, p.MinPrincipal, p.MaxPrincipal,
                p.MinTermMonths, p.MaxTermMonths, p.RepaymentFrequency, p.GracePeriodDays, p.PenaltyRatePct, p.RequiresCollateral, p.RequiresCoMaker,
                p.LoanReceivableAccountId, p.InterestIncomeAccountId, p.PenaltyIncomeAccountId, p.Status, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(products);
    }
}
