namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteWithholdingTaxBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteWithholdingTaxBracketCommand>
{
    public async Task<Result> HandleAsync(DeleteWithholdingTaxBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.WithholdingTaxBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("WithholdingTaxBracket.NotFound", $"Withholding tax bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("WithholdingTaxBracket.Forbidden", "You do not have permission to delete withholding tax brackets."));

        dbContext.WithholdingTaxBrackets.Remove(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
