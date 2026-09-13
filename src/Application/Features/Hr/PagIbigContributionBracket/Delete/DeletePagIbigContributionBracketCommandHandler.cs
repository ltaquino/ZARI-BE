namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePagIbigContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeletePagIbigContributionBracketCommand>
{
    public async Task<Result> HandleAsync(DeletePagIbigContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.PagIbigContributionBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("PagIbigContributionBracket.NotFound", $"Pag-IBIG contribution bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("PagIbigContributionBracket.Forbidden", "You do not have permission to delete Pag-IBIG contribution brackets."));

        dbContext.PagIbigContributionBrackets.Remove(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
