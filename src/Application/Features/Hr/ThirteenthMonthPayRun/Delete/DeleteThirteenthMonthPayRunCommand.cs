namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteThirteenthMonthPayRunCommand(Guid Id) : ICommand;
