namespace ZARI.Application.Features.Accounting.CostCenters.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCostCenterCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteCostCenterCommand>
{
    public async Task<Result> HandleAsync(DeleteCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        var costCenter = await dbContext.CostCenters.FindAsync([command.Id], cancellationToken);
        if (costCenter is null)
            return Result.Failure(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.Id}' was not found."));

        var hasPermission = costCenter.BranchId is not null
            ? await permissionService.HasPermissionOnBranchAsync("COST_CENTERS", FormAction.Delete, costCenter.BranchId, cancellationToken)
            : await permissionService.HasPermissionAsync("COST_CENTERS", FormAction.Delete, cancellationToken);
        if (!hasPermission)
            return Result.Failure(Error.Forbidden("CostCenter.Forbidden", "You do not have permission to delete cost centers for this branch."));

        dbContext.CostCenters.Remove(costCenter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
