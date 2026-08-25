namespace ZARI.Application.Features.Accounting.TaxCodes.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteTaxCodeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteTaxCodeCommand>
{
    public async Task<Result> HandleAsync(DeleteTaxCodeCommand command, CancellationToken cancellationToken = default)
    {
        var taxCode = await dbContext.TaxCodes.FindAsync([command.Code], cancellationToken);
        if (taxCode is null)
            return Result.Failure(Error.NotFound("TaxCode.NotFound", $"Tax code '{command.Code}' was not found."));

        if (!await permissionService.HasPermissionAsync("TAX_CODES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("TaxCode.Forbidden", "You do not have permission to delete tax codes."));

        dbContext.TaxCodes.Remove(taxCode);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
