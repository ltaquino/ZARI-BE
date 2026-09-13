namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Compute;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Common;

public sealed record ComputeThirteenthMonthPayRunCommand(Guid Id) : ICommand<Result<ThirteenthMonthPayRunResponse>>;
