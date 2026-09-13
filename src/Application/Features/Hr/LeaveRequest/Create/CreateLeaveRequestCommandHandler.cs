namespace ZARI.Application.Features.Hr.LeaveRequests.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Hr.LeaveRequests.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLeaveRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLeaveRequestCommand, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> HandleAsync(CreateLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<LeaveRequestResponse>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to create leave requests for this branch."));

        var leaveTypeExists = await dbContext.LeaveTypes.AnyAsync(l => l.Id == command.LeaveTypeId, cancellationToken);
        if (!leaveTypeExists)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{command.LeaveTypeId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(employee.BranchId, "LEAVE-REQ"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(numberResult.Error!);

        var days = await LeaveRequestDaysCalculator.CalculateAsync(dbContext, command.EmployeeId, command.DateFrom, command.DateTo, cancellationToken);

        var request = new LeaveRequest
        {
            RequestNo = numberResult.Value!.DocumentNumber,
            EmployeeId = command.EmployeeId,
            LeaveTypeId = command.LeaveTypeId,
            DateFrom = command.DateFrom,
            DateTo = command.DateTo,
            Days = days,
            Reason = command.Reason,
            AttachmentUrl = command.AttachmentUrl,
            Status = "DRAFT"
        };

        dbContext.LeaveRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new LeaveRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.LeaveTypeId, request.DateFrom, request.DateTo, request.Days, request.Reason, request.AttachmentUrl, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
