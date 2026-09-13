namespace ZARI.Application.Features.Hr.OvertimeRequests.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteOvertimeRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteOvertimeRequestCommand>
{
    public async Task<Result> HandleAsync(DeleteOvertimeRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.OvertimeRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("OvertimeRequest.NotFound", $"Overtime request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to delete overtime requests for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("OvertimeRequest.NotDraft", "Only draft overtime requests can be deleted."));

        dbContext.OvertimeRequests.Remove(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
