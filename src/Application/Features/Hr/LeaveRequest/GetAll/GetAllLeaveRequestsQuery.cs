namespace ZARI.Application.Features.Hr.LeaveRequests.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed record GetAllLeaveRequestsQuery : IQuery<Result<List<LeaveRequestResponse>>>;
