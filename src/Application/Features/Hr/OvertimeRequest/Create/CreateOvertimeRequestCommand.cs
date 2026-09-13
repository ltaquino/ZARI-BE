namespace ZARI.Application.Features.Hr.OvertimeRequests.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed record CreateOvertimeRequestCommand(
    Guid EmployeeId,
    DateTimeOffset Date,
    DateTimeOffset PlannedTimeFrom,
    DateTimeOffset PlannedTimeTo,
    string OtType,
    string? Reason) : ICommand<Result<OvertimeRequestResponse>>;
