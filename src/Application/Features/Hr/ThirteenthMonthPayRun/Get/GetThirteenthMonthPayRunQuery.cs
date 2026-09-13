namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetThirteenthMonthPayRunQuery(Guid Id) : IQuery<Result<ThirteenthMonthPayRunResponse>>;

public sealed record ThirteenthMonthPayLineResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    decimal TotalBasicPay,
    decimal ThirteenthMonthPay);

public sealed record ThirteenthMonthPayRunResponse(
    Guid Id,
    string RunNo,
    string BranchId,
    int Year,
    DateTimeOffset PayDate,
    string Status,
    DateTimeOffset CreatedAt,
    List<ThirteenthMonthPayLineResponse> Lines);
