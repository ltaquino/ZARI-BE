namespace ZARI.Application.Features.Hr.Positions.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePositionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreatePositionCommand, Result<PositionResponse>>
{
    public async Task<Result<PositionResponse>> HandleAsync(CreatePositionCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POSITIONS", FormAction.Create, cancellationToken))
            return Result.Failure<PositionResponse>(Error.Forbidden("Position.Forbidden", "You do not have permission to create positions."));

        var codeExists = await dbContext.Positions.AnyAsync(p => p.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<PositionResponse>(Error.Conflict("Position.DuplicateCode", $"A position with code '{command.Code}' already exists."));

        var departmentExists = await dbContext.Departments.AnyAsync(d => d.Id == command.DepartmentId, cancellationToken);
        if (!departmentExists)
            return Result.Failure<PositionResponse>(Error.NotFound("Department.NotFound", $"Department with ID '{command.DepartmentId}' was not found."));

        var position = new Position
        {
            Code = command.Code,
            Name = command.Name,
            DepartmentId = command.DepartmentId,
            Status = command.Status,
            PayGradeMin = command.PayGradeMin,
            PayGradeMax = command.PayGradeMax
        };

        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PositionResponse(position.Id, position.Code, position.Name, position.DepartmentId, position.Status, position.PayGradeMin, position.PayGradeMax, position.CreatedAt);
        return Result.Success(response);
    }
}
