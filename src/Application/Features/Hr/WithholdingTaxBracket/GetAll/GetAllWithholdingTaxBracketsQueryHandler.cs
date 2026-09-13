namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Domain.Common;

public sealed class GetAllWithholdingTaxBracketsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllWithholdingTaxBracketsQuery, Result<List<WithholdingTaxBracketResponse>>>
{
    public async Task<Result<List<WithholdingTaxBracketResponse>>> HandleAsync(GetAllWithholdingTaxBracketsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<List<WithholdingTaxBracketResponse>>(Error.Forbidden("WithholdingTaxBracket.Forbidden", "You do not have permission to view withholding tax brackets."));

        var items = await dbContext.WithholdingTaxBrackets.AsNoTracking()
            .OrderByDescending(w => w.EffectiveDate).ThenBy(w => w.SalaryFrom)
            .Select(w => new WithholdingTaxBracketResponse(w.Id, w.SalaryFrom, w.SalaryTo, w.BaseTax, w.RateOverExcess, w.EffectiveDate, w.Status, w.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
