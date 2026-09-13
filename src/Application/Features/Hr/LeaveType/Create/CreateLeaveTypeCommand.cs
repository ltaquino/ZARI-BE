namespace ZARI.Application.Features.Hr.LeaveTypes.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Domain.Common;

public sealed record CreateLeaveTypeCommand(
    string Code,
    string Name,
    string AccrualFrequency,
    decimal AccrualAmount,
    decimal? MaxCarryOverDays,
    bool IsPaid,
    bool RequiresDocumentation,
    string Status) : ICommand<Result<LeaveTypeResponse>>;
