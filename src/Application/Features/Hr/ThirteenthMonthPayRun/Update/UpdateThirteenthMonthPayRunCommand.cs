namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateThirteenthMonthPayRunCommand(Guid Id, DateTimeOffset PayDate) : ICommand;
