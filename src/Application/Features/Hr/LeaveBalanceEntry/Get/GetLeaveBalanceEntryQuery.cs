namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetLeaveBalanceEntryQuery(Guid Id) : IQuery<Result<LeaveBalanceEntryResponse>>;

public sealed record LeaveBalanceEntryResponse(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTimeOffset Date,
    string Type,
    decimal Days,
    decimal RunningBalance,
    string? ReferenceTable,
    string? ReferenceId,
    DateTimeOffset CreatedAt);
