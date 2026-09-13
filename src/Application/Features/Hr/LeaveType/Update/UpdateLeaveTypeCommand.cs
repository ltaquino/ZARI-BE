namespace ZARI.Application.Features.Hr.LeaveTypes.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateLeaveTypeCommand(
    Guid Id,
    string Code,
    string Name,
    string AccrualFrequency,
    decimal AccrualAmount,
    decimal? MaxCarryOverDays,
    bool IsPaid,
    bool RequiresDocumentation,
    string Status) : ICommand;
