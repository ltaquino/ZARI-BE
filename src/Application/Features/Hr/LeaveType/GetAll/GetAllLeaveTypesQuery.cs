namespace ZARI.Application.Features.Hr.LeaveTypes.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Domain.Common;

public sealed record GetAllLeaveTypesQuery : IQuery<Result<List<LeaveTypeResponse>>>;
