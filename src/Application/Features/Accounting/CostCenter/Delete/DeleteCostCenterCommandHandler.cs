namespace ZARI.Application.Features.Accounting.CostCenters.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCostCenterCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteCostCenterCommand>
{
    public async Task<Result> HandleAsync(DeleteCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        var costCenter = await dbContext.CostCenters.FindAsync([command.Id], cancellationToken);
        if (costCenter is null)
            return Result.Failure(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.Id}' was not found."));

        dbContext.CostCenters.Remove(costCenter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
