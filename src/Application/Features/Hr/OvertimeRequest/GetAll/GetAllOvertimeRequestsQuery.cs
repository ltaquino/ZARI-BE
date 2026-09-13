namespace ZARI.Application.Features.Hr.OvertimeRequests.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed record GetAllOvertimeRequestsQuery : IQuery<Result<List<OvertimeRequestResponse>>>;
