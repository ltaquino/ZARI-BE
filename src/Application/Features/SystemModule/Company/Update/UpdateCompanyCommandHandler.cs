namespace ZARI.Application.Features.SystemModule.Companies.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCompanyCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateCompanyCommand>
{
    public async Task<Result> HandleAsync(UpdateCompanyCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("COMPANY", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Company.Forbidden", "You do not have permission to update company settings."));

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        if (company is null)
            return Result.Failure(Error.NotFound("Company.NotFound", "No company record is configured."));

        company.Code = command.Code;
        company.Name = command.Name;
        company.TaxId = command.TaxId;
        company.BaseCurrencyId = command.BaseCurrencyId;
        company.RegisteredAddress = command.RegisteredAddress;
        company.TradeName = command.TradeName;
        company.VatRegistrationType = command.VatRegistrationType;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
