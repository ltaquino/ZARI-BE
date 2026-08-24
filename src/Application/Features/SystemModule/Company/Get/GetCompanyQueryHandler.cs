namespace ZARI.Application.Features.SystemModule.Companies.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCompanyQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetCompanyQuery, Result<CompanyResponse>>
{
    public async Task<Result<CompanyResponse>> HandleAsync(GetCompanyQuery query, CancellationToken cancellationToken = default)
    {
        var company = await dbContext.Companies
            .Select(c => new CompanyResponse(c.Id, c.Code, c.Name, c.TaxId, c.BaseCurrencyId, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            return Result.Failure<CompanyResponse>(Error.NotFound("Company.NotFound", "No company record is configured."));

        return Result.Success(company);
    }
}
