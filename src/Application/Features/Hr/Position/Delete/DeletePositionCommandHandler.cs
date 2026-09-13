namespace ZARI.Application.Features.Hr.Positions.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePositionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeletePositionCommand>
{
    public async Task<Result> HandleAsync(DeletePositionCommand command, CancellationToken cancellationToken = default)
    {
        var position = await dbContext.Positions.FindAsync([command.Id], cancellationToken);
        if (position is null)
            return Result.Failure(Error.NotFound("Position.NotFound", $"Position with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("POSITIONS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Position.Forbidden", "You do not have permission to delete positions."));

        var employeeCount = await dbContext.Employees.CountAsync(e => e.PositionId == command.Id, cancellationToken);
        if (employeeCount > 0)
            return Result.Failure(Error.Conflict("Position.HasEmployees", $"Cannot delete this position — it has {employeeCount} employee{(employeeCount == 1 ? "" : "s")}."));

        dbContext.Positions.Remove(position);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
