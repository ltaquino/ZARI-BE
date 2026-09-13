namespace ZARI.Application.Features.Hr.SssContributionBrackets.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteSssContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteSssContributionBracketCommand>
{
    public async Task<Result> HandleAsync(DeleteSssContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.SssContributionBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("SssContributionBracket.NotFound", $"SSS contribution bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("SssContributionBracket.Forbidden", "You do not have permission to delete SSS contribution brackets."));

        dbContext.SssContributionBrackets.Remove(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
