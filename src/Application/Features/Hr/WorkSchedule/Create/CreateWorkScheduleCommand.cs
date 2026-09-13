namespace ZARI.Application.Features.Hr.WorkSchedules.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Domain.Common;

public sealed record CreateWorkScheduleCommand(
    string Code,
    string Name,
    TimeOnly TimeIn,
    TimeOnly TimeOut,
    string WorkDays,
    int GracePeriodMinutes,
    int BreakMinutes,
    string Status) : ICommand<Result<WorkScheduleResponse>>;
