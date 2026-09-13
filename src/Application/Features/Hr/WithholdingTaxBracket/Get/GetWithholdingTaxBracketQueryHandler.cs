namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetWithholdingTaxBracketQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetWithholdingTaxBracketQuery, Result<WithholdingTaxBracketResponse>>
{
    public async Task<Result<WithholdingTaxBracketResponse>> HandleAsync(GetWithholdingTaxBracketQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.View, cancellationToken))
            return Result.Failure<WithholdingTaxBracketResponse>(Error.Forbidden("WithholdingTaxBracket.Forbidden", "You do not have permission to view withholding tax brackets."));

        var bracket = await dbContext.WithholdingTaxBrackets
            .Where(w => w.Id == query.Id)
            .Select(w => new WithholdingTaxBracketResponse(w.Id, w.SalaryFrom, w.SalaryTo, w.BaseTax, w.RateOverExcess, w.EffectiveDate, w.Status, w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (bracket is null)
            return Result.Failure<WithholdingTaxBracketResponse>(Error.NotFound("WithholdingTaxBracket.NotFound", $"Withholding tax bracket with ID '{query.Id}' was not found."));

        return Result.Success(bracket);
    }
}
