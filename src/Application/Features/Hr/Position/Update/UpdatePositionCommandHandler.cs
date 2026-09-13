namespace ZARI.Application.Features.Hr.Positions.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePositionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdatePositionCommand>
{
    public async Task<Result> HandleAsync(UpdatePositionCommand command, CancellationToken cancellationToken = default)
    {
        var position = await dbContext.Positions.FindAsync([command.Id], cancellationToken);
        if (position is null)
            return Result.Failure(Error.NotFound("Position.NotFound", $"Position with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("POSITIONS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Position.Forbidden", "You do not have permission to update positions."));

        var duplicateCode = await dbContext.Positions
            .AnyAsync(p => p.Id != command.Id && p.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("Position.DuplicateCode", $"A position with code '{command.Code}' already exists."));

        var departmentExists = await dbContext.Departments.AnyAsync(d => d.Id == command.DepartmentId, cancellationToken);
        if (!departmentExists)
            return Result.Failure(Error.NotFound("Department.NotFound", $"Department with ID '{command.DepartmentId}' was not found."));

        position.Code = command.Code;
        position.Name = command.Name;
        position.DepartmentId = command.DepartmentId;
        position.Status = command.Status;
        position.PayGradeMin = command.PayGradeMin;
        position.PayGradeMax = command.PayGradeMax;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
