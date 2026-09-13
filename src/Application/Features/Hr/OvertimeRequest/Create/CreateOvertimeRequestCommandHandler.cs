namespace ZARI.Application.Features.Hr.OvertimeRequests.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateOvertimeRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateOvertimeRequestCommand, Result<OvertimeRequestResponse>>
{
    public async Task<Result<OvertimeRequestResponse>> HandleAsync(CreateOvertimeRequestCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<OvertimeRequestResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<OvertimeRequestResponse>(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to create overtime requests for this branch."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(employee.BranchId, "OT-REQ"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<OvertimeRequestResponse>(numberResult.Error!);

        var request = new OvertimeRequest
        {
            RequestNo = numberResult.Value!.DocumentNumber,
            EmployeeId = command.EmployeeId,
            Date = command.Date,
            PlannedTimeFrom = command.PlannedTimeFrom,
            PlannedTimeTo = command.PlannedTimeTo,
            OtType = command.OtType,
            Reason = command.Reason,
            Status = "DRAFT"
        };

        dbContext.OvertimeRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new OvertimeRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, request.OtType, request.Reason, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
