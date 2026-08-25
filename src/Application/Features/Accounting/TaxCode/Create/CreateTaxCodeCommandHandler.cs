namespace ZARI.Application.Features.Accounting.TaxCodes.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateTaxCodeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateTaxCodeCommand, Result<TaxCodeResponse>>
{
    public async Task<Result<TaxCodeResponse>> HandleAsync(CreateTaxCodeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("TAX_CODES", FormAction.Create, cancellationToken))
            return Result.Failure<TaxCodeResponse>(Error.Forbidden("TaxCode.Forbidden", "You do not have permission to create tax codes."));

        var codeExists = await dbContext.TaxCodes.AnyAsync(t => t.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<TaxCodeResponse>(Error.Conflict("TaxCode.DuplicateCode", $"A tax code '{command.Code}' already exists."));

        if (command.GlAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.GlAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure<TaxCodeResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.GlAccountId}' was not found."));
        }

        var taxCode = new TaxCode
        {
            Code = command.Code,
            Name = command.Name,
            Rate = command.Rate,
            TaxType = command.TaxType,
            GlAccountId = command.GlAccountId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.TaxCodes.Add(taxCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new TaxCodeResponse(taxCode.Code, taxCode.Code, taxCode.Name, taxCode.Rate, taxCode.TaxType, taxCode.GlAccountId, taxCode.CreatedAt);
        return Result.Success(response);
    }
}
