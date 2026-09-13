namespace ZARI.Application.Features.Hr.DailyTimeRecords.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDailyTimeRecordCommand(Guid Id) : ICommand;
