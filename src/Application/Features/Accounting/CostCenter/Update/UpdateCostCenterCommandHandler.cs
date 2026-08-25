namespace ZARI.Application.Features.Accounting.CostCenters.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCostCenterCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateCostCenterCommand>
{
    public async Task<Result> HandleAsync(UpdateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        var costCenter = await dbContext.CostCenters.FindAsync([command.Id], cancellationToken);
        if (costCenter is null)
            return Result.Failure(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.Id}' was not found."));

        var hasPermission = costCenter.BranchId is not null
            ? await permissionService.HasPermissionOnBranchAsync("COST_CENTERS", FormAction.Edit, costCenter.BranchId, cancellationToken)
            : await permissionService.HasPermissionAsync("COST_CENTERS", FormAction.Edit, cancellationToken);
        if (!hasPermission)
            return Result.Failure(Error.Forbidden("CostCenter.Forbidden", "You do not have permission to update cost centers for this branch."));

        var duplicateCode = await dbContext.CostCenters
            .AnyAsync(c => c.Id != command.Id && c.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("CostCenter.DuplicateCode", $"A cost center with code '{command.Code}' already exists."));

        if (command.BranchId is not null)
        {
            var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
            if (!branchExists)
                return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));
        }

        costCenter.BranchId = command.BranchId;
        costCenter.Code = command.Code;
        costCenter.Name = command.Name;
        costCenter.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
