namespace ZARI.Application.Features.Hr.LeaveRequests.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLeaveRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteLeaveRequestCommand>
{
    public async Task<Result> HandleAsync(DeleteLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.LeaveRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to delete leave requests for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("LeaveRequest.NotDraft", "Only draft leave requests can be deleted."));

        dbContext.LeaveRequests.Remove(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
