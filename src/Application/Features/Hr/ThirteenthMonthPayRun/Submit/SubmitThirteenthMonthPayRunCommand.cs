namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Common;

public sealed record SubmitThirteenthMonthPayRunCommand(Guid Id, string RequestedBy) : ICommand<Result<ThirteenthMonthPayRunResponse>>;
