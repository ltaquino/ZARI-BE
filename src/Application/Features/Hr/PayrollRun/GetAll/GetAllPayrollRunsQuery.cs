namespace ZARI.Application.Features.Hr.PayrollRuns.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllPayrollRunsQuery : IQuery<Result<List<PayrollRunSummaryResponse>>>;

public sealed record PayrollRunSummaryResponse(
    Guid Id,
    string PayrollRunNo,
    string BranchId,
    DateTimeOffset PayPeriodStart,
    DateTimeOffset PayPeriodEnd,
    DateTimeOffset PayDate,
    string Status,
    int LineCount,
    DateTimeOffset CreatedAt);
