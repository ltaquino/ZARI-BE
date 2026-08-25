namespace ZARI.Application.Features.Accounting.TaxCodes.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateTaxCodeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateTaxCodeCommand>
{
    public async Task<Result> HandleAsync(UpdateTaxCodeCommand command, CancellationToken cancellationToken = default)
    {
        var taxCode = await dbContext.TaxCodes.FindAsync([command.Code], cancellationToken);
        if (taxCode is null)
            return Result.Failure(Error.NotFound("TaxCode.NotFound", $"Tax code '{command.Code}' was not found."));

        if (!await permissionService.HasPermissionAsync("TAX_CODES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("TaxCode.Forbidden", "You do not have permission to update tax codes."));

        if (command.GlAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.GlAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.GlAccountId}' was not found."));
        }

        taxCode.Name = command.Name;
        taxCode.Rate = command.Rate;
        taxCode.TaxType = command.TaxType;
        taxCode.GlAccountId = command.GlAccountId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
