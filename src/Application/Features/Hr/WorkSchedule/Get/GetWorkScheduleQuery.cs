namespace ZARI.Application.Features.Hr.WorkSchedules.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetWorkScheduleQuery(Guid Id) : IQuery<Result<WorkScheduleResponse>>;

public sealed record WorkScheduleResponse(
    Guid Id,
    string Code,
    string Name,
    TimeOnly TimeIn,
    TimeOnly TimeOut,
    string WorkDays,
    int GracePeriodMinutes,
    int BreakMinutes,
    string Status,
    DateTimeOffset CreatedAt);
