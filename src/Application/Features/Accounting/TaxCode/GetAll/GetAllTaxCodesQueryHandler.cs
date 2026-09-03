namespace ZARI.Application.Features.Accounting.TaxCodes.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Domain.Common;

public sealed class GetAllTaxCodesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllTaxCodesQuery, Result<List<TaxCodeResponse>>>
{
    public async Task<Result<List<TaxCodeResponse>>> HandleAsync(GetAllTaxCodesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("TAX_CODES", FormAction.View, cancellationToken))
            return Result.Failure<List<TaxCodeResponse>>(Error.Forbidden("TaxCode.Forbidden", "You do not have permission to view tax codes."));

        var items = await dbContext.TaxCodes.AsNoTracking()
            .OrderBy(t => t.Code)
            .Select(t => new TaxCodeResponse(t.Code, t.Code, t.Name, t.Rate, t.TaxType, t.GlAccountId, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
