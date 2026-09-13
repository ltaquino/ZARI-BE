namespace ZARI.Application.Features.Hr.OvertimeRequests.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Only DRAFT requests can be edited — matches LeaveRequest's convention.</summary>
public sealed class UpdateOvertimeRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateOvertimeRequestCommand>
{
    public async Task<Result> HandleAsync(UpdateOvertimeRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.OvertimeRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("OvertimeRequest.NotFound", $"Overtime request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to update overtime requests for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("OvertimeRequest.NotDraft", "Only draft overtime requests can be edited."));

        request.Date = command.Date;
        request.PlannedTimeFrom = command.PlannedTimeFrom;
        request.PlannedTimeTo = command.PlannedTimeTo;
        request.OtType = command.OtType;
        request.Reason = command.Reason;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
