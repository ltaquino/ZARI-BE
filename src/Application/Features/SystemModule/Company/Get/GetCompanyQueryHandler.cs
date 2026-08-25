namespace ZARI.Application.Features.SystemModule.Companies.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCompanyQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetCompanyQuery, Result<CompanyResponse>>
{
    public async Task<Result<CompanyResponse>> HandleAsync(GetCompanyQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("COMPANY", FormAction.View, cancellationToken))
            return Result.Failure<CompanyResponse>(Error.Forbidden("Company.Forbidden", "You do not have permission to view the company."));

        var company = await dbContext.Companies
            .Select(c => new CompanyResponse(c.Id, c.Code, c.Name, c.TaxId, c.BaseCurrencyId, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            return Result.Failure<CompanyResponse>(Error.NotFound("Company.NotFound", "No company record is configured."));

        return Result.Success(company);
    }
}
