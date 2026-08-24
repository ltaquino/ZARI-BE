namespace ZARI.Application.Features.SystemModule.Companies.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCompanyCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateCompanyCommand>
{
    public async Task<Result> HandleAsync(UpdateCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        if (company is null)
            return Result.Failure(Error.NotFound("Company.NotFound", "No company record is configured."));

        company.Code = command.Code;
        company.Name = command.Name;
        company.TaxId = command.TaxId;
        company.BaseCurrencyId = command.BaseCurrencyId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
