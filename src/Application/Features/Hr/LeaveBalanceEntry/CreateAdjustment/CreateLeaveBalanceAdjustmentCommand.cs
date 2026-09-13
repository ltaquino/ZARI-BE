namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.CreateAdjustment;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Domain.Common;

/// <summary>
/// HR manually posts an ACCRUAL/CARRYOVER/ADJUSTMENT row (e.g. the periodic entitlement credit, or
/// a correction) — USAGE rows are never posted through this endpoint, only automatically by
/// ApproveLeaveRequestCommandHandler. No automated monthly accrual job exists yet (out of scope for
/// this build-order step); this is the manual fallback until one is built.
/// </summary>
public sealed record CreateLeaveBalanceAdjustmentCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTimeOffset Date,
    string Type,
    decimal Days,
    string? ReferenceTable,
    string? ReferenceId) : ICommand<Result<LeaveBalanceEntryResponse>>;
