namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllThirteenthMonthPayRunsQuery : IQuery<Result<List<ThirteenthMonthPayRunSummaryResponse>>>;

public sealed record ThirteenthMonthPayRunSummaryResponse(
    Guid Id,
    string RunNo,
    string BranchId,
    int Year,
    DateTimeOffset PayDate,
    string Status,
    int LineCount,
    DateTimeOffset CreatedAt);
