namespace ZARI.Application.Features.Hr.LeaveTypes.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetLeaveTypeQuery(Guid Id) : IQuery<Result<LeaveTypeResponse>>;

public sealed record LeaveTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string AccrualFrequency,
    decimal AccrualAmount,
    decimal? MaxCarryOverDays,
    bool IsPaid,
    bool RequiresDocumentation,
    string Status,
    DateTimeOffset CreatedAt);
