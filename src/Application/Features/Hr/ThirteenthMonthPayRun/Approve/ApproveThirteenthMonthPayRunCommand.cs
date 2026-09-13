namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Common;

public sealed record ApproveThirteenthMonthPayRunCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<ThirteenthMonthPayRunResponse>>;
