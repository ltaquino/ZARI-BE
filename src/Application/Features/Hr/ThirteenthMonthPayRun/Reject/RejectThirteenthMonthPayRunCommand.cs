namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Common;

public sealed record RejectThirteenthMonthPayRunCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<ThirteenthMonthPayRunResponse>>;
