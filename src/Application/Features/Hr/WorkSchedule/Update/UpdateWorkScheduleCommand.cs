namespace ZARI.Application.Features.Hr.WorkSchedules.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateWorkScheduleCommand(
    Guid Id,
    string Code,
    string Name,
    TimeOnly TimeIn,
    TimeOnly TimeOut,
    string WorkDays,
    int GracePeriodMinutes,
    int BreakMinutes,
    string Status) : ICommand;
