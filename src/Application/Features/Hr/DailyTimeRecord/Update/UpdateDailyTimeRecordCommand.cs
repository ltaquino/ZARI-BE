namespace ZARI.Application.Features.Hr.DailyTimeRecords.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateDailyTimeRecordCommand(
    Guid Id,
    DateTimeOffset? TimeIn,
    DateTimeOffset? TimeOut,
    DateTimeOffset? BreakOutTime,
    DateTimeOffset? BreakInTime,
    string Source,
    string? Remarks) : ICommand;
