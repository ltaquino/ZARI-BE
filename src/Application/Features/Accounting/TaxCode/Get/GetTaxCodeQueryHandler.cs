namespace ZARI.Application.Features.Accounting.TaxCodes.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetTaxCodeQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetTaxCodeQuery, Result<TaxCodeResponse>>
{
    public async Task<Result<TaxCodeResponse>> HandleAsync(GetTaxCodeQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("TAX_CODES", FormAction.View, cancellationToken))
            return Result.Failure<TaxCodeResponse>(Error.Forbidden("TaxCode.Forbidden", "You do not have permission to view tax codes."));

        var taxCode = await dbContext.TaxCodes
            .Where(t => t.Code == query.Code)
            .Select(t => new TaxCodeResponse(t.Code, t.Code, t.Name, t.Rate, t.TaxType, t.GlAccountId, t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (taxCode is null)
            return Result.Failure<TaxCodeResponse>(Error.NotFound("TaxCode.NotFound", $"Tax code '{query.Code}' was not found."));

        return Result.Success(taxCode);
    }
}
