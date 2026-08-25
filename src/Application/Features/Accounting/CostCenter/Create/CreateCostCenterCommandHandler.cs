namespace ZARI.Application.Features.Accounting.CostCenters.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCostCenterCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateCostCenterCommand, Result<CostCenterResponse>>
{
    public async Task<Result<CostCenterResponse>> HandleAsync(CreateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        var hasPermission = command.BranchId is not null
            ? await permissionService.HasPermissionOnBranchAsync("COST_CENTERS", FormAction.Create, command.BranchId, cancellationToken)
            : await permissionService.HasPermissionAsync("COST_CENTERS", FormAction.Create, cancellationToken);
        if (!hasPermission)
            return Result.Failure<CostCenterResponse>(Error.Forbidden("CostCenter.Forbidden", "You do not have permission to create cost centers for this branch."));

        var codeExists = await dbContext.CostCenters.AnyAsync(c => c.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<CostCenterResponse>(Error.Conflict("CostCenter.DuplicateCode", $"A cost center with code '{command.Code}' already exists."));

        if (command.BranchId is not null)
        {
            var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
            if (!branchExists)
                return Result.Failure<CostCenterResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));
        }

        var costCenter = new CostCenter
        {
            BranchId = command.BranchId,
            Code = command.Code,
            Name = command.Name,
            Status = command.Status
        };

        dbContext.CostCenters.Add(costCenter);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CostCenterResponse(costCenter.Id, costCenter.BranchId, costCenter.Code, costCenter.Name, costCenter.Status, costCenter.CreatedAt);
        return Result.Success(response);
    }
}
