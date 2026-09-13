namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Common;

public sealed record CreateThirteenthMonthPayRunCommand(
    string BranchId,
    int Year,
    DateTimeOffset PayDate) : ICommand<Result<ThirteenthMonthPayRunResponse>>;
