namespace ZARI.Application.Features.Hr.OvertimeRequests.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed record ApproveOvertimeRequestCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<OvertimeRequestResponse>>;
