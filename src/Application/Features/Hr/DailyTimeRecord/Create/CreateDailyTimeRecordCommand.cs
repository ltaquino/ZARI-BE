namespace ZARI.Application.Features.Hr.DailyTimeRecords.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Domain.Common;

public sealed record CreateDailyTimeRecordCommand(
    Guid EmployeeId,
    DateTimeOffset Date,
    DateTimeOffset? TimeIn,
    DateTimeOffset? TimeOut,
    DateTimeOffset? BreakOutTime,
    DateTimeOffset? BreakInTime,
    string Source,
    string? Remarks) : ICommand<Result<DailyTimeRecordResponse>>;
