namespace ZARI.Application.Features.Hr.OvertimeRequests.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed record SubmitOvertimeRequestCommand(Guid Id, string RequestedBy) : ICommand<Result<OvertimeRequestResponse>>;
